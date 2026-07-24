using GestionCelulares.Application.Apartados;
using GestionCelulares.Application.Common;
using GestionCelulares.Application.Common.Interfaces;
using GestionCelulares.Application.Ventas;
using GestionCelulares.Domain.Entities;
using GestionCelulares.Infrastructure.Persistence;
using GestionCelulares.Tests.Infra;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionCelulares.Tests;

public class ApartadoServiceTests
{
    private const int Cliente = 1, Imei = 10, MetodoEfectivo = 1, Usuario = 7;

    private sealed class VentaProcStub : IVentaProcedures
    {
        public Func<int>? AlRegistrar;
        public Task<int> RegistrarAsync(VentaRegistroDto dto, int usuarioId, int? sesionCajaId)
            => Task.FromResult(AlRegistrar?.Invoke() ?? 500);
        public Task<string> ProximoNumeroFacturaAsync() => Task.FromResult("FC-0001");
    }

    private static (GestionCelularesContext db, ApartadoService svc, VentaProcStub proc) Crear(bool cajaAbierta = true)
    {
        var db = TestDb.Crear();
        db.Empresas.Add(new Empresa { EmpresaId = 1, Nombre = "Tienda", RNC = "131", PorcentajeItbis = 18m });
        db.Sucursales.Add(new Sucursal { SucursalId = 1, Nombre = "Principal" });
        db.Clientes.Add(new Cliente { ClienteId = Cliente, Nombre = "Juan", Cedula = "00100000001" });
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = MetodoEfectivo, Nombre = "Efectivo" });
        db.MetodosPago.Add(new MetodoPago { MetodoPagoId = 2, Nombre = "Apartado" });
        var p = new Producto { ProductoId = 1, Nombre = "iPhone", Activo = true };
        p.Variantes.Add(new ProductoVariante { VarianteId = 1, ProductoId = 1, PrecioVenta = 10000m, PrecioCosto = 8000m, Activo = true });
        db.Productos.Add(p);
        db.InventarioImeis.Add(new InventarioImei { ImeiId = Imei, Imei = "IMEI-A", VarianteId = 1, SucursalId = 1, Estado = "Disponible", PrecioCosto = 8000m });
        if (cajaAbierta)
            db.SesionesCaja.Add(new SesionCaja { SucursalId = 1, UsuarioApertura = Usuario, MontoApertura = 0m, Estado = "Abierta", FechaApertura = DateTime.Now });
        db.SaveChanges();
        var proc = new VentaProcStub();
        return (db, new ApartadoService(db, proc), proc);
    }

    private static ApartadoCrearDto CrearDto(decimal precio = 10000m, decimal abono = 0m, int? metodo = MetodoEfectivo) =>
        new() { ClienteId = Cliente, ImeiId = Imei, PrecioTotal = precio, AbonoInicial = abono, MetodoPagoId = metodo };

    // ---------- CrearAsync ----------

    [Fact]
    public async Task Crear_reserva_el_equipo_y_registra_abono_inicial()
    {
        var (db, svc, _) = Crear();

        var ap = await svc.CrearAsync(CrearDto(abono: 3000m), Usuario);

        Assert.Equal("Activo", ap.Estado);
        Assert.Equal(3000m, ap.TotalAbonado);
        Assert.Equal(7000m, ap.Saldo);
        Assert.Equal("Apartado", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == Imei)).Estado);
        Assert.Single(await db.AbonosApartado.ToListAsync());
    }

    [Fact]
    public async Task Crear_con_equipo_no_disponible_falla()
    {
        var (db, svc, _) = Crear();
        (await db.InventarioImeis.FirstAsync(i => i.ImeiId == Imei)).Estado = "Vendido";
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ApartadoException>(() => svc.CrearAsync(CrearDto(), Usuario));
    }

    [Fact]
    public async Task Crear_con_abono_mayor_al_precio_falla()
    {
        var (_, svc, _) = Crear();
        await Assert.ThrowsAsync<ApartadoException>(() => svc.CrearAsync(CrearDto(precio: 5000m, abono: 6000m), Usuario));
    }

    [Fact]
    public async Task Crear_con_abono_sin_caja_abierta_falla()
    {
        var (_, svc, _) = Crear(cajaAbierta: false);
        await Assert.ThrowsAsync<ApartadoException>(() => svc.CrearAsync(CrearDto(abono: 1000m), Usuario));
    }

    // ---------- AbonarAsync ----------

    [Fact]
    public async Task Abonar_suma_al_total_abonado()
    {
        var (_, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(abono: 2000m), Usuario);

        var actualizado = await svc.AbonarAsync(ap.ApartadoId, new AbonoApartadoCrearDto { Monto = 3000m, MetodoPagoId = MetodoEfectivo }, Usuario);

        Assert.Equal(5000m, actualizado.TotalAbonado);
        Assert.Equal(5000m, actualizado.Saldo);
    }

    [Fact]
    public async Task Abonar_mas_que_el_saldo_falla()
    {
        var (_, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(precio: 5000m, abono: 4000m), Usuario);

        await Assert.ThrowsAsync<ApartadoException>(() =>
            svc.AbonarAsync(ap.ApartadoId, new AbonoApartadoCrearDto { Monto = 2000m, MetodoPagoId = MetodoEfectivo }, Usuario));
    }

    // ---------- CompletarAsync ----------

    [Fact]
    public async Task Completar_con_saldo_pendiente_falla()
    {
        var (_, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(abono: 5000m), Usuario);  // debe 5000

        await Assert.ThrowsAsync<ApartadoException>(() => svc.CompletarAsync(ap.ApartadoId, Usuario));
    }

    [Fact]
    public async Task Completar_pagado_genera_venta_y_cierra()
    {
        var (db, svc, proc) = Crear();
        proc.AlRegistrar = () => 777;
        var ap = await svc.CrearAsync(CrearDto(abono: 10000m), Usuario);  // pagado completo

        var res = await svc.CompletarAsync(ap.ApartadoId, Usuario);

        Assert.Equal("Completado", res.Estado);
        Assert.Equal(777, res.VentaId);
        Assert.NotNull(res.FechaCierre);
    }

    [Fact]
    public async Task Completar_revierte_la_reserva_si_la_venta_falla()
    {
        var (db, svc, proc) = Crear();
        proc.AlRegistrar = () => throw new InvalidOperationException("SP falló");
        var ap = await svc.CrearAsync(CrearDto(abono: 10000m), Usuario);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.CompletarAsync(ap.ApartadoId, Usuario));

        // El equipo vuelve a quedar reservado (Apartado), no perdido en "Disponible".
        Assert.Equal("Apartado", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == Imei)).Estado);
        Assert.Equal("Activo", (await db.Apartados.FirstAsync(a => a.ApartadoId == ap.ApartadoId)).Estado);
    }

    // ---------- CancelarAsync ----------

    [Fact]
    public async Task Cancelar_libera_el_equipo()
    {
        var (db, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(abono: 1000m), Usuario);

        var res = await svc.CancelarAsync(ap.ApartadoId, new CancelarApartadoDto { DevolverMonto = 0m }, Usuario);

        Assert.Equal("Cancelado", res.Estado);
        Assert.Equal("Disponible", (await db.InventarioImeis.FirstAsync(i => i.ImeiId == Imei)).Estado);
    }

    [Fact]
    public async Task Cancelar_con_devolucion_registra_egreso_de_caja()
    {
        var (db, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(abono: 3000m), Usuario);

        await svc.CancelarAsync(ap.ApartadoId, new CancelarApartadoDto { DevolverMonto = 3000m, Motivo = "arrepentido" }, Usuario);

        var egreso = await db.MovimientosCaja.SingleAsync(m => m.Tipo == "Egreso");
        Assert.Equal(3000m, egreso.Monto);
        Assert.Contains("Devolucion", (await db.AbonosApartado.Select(b => b.Tipo).ToListAsync()));
    }

    [Fact]
    public async Task Cancelar_con_devolucion_mayor_a_lo_abonado_falla()
    {
        var (_, svc, _) = Crear();
        var ap = await svc.CrearAsync(CrearDto(abono: 2000m), Usuario);

        await Assert.ThrowsAsync<ApartadoException>(() =>
            svc.CancelarAsync(ap.ApartadoId, new CancelarApartadoDto { DevolverMonto = 3000m }, Usuario));
    }
}
