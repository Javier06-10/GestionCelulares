// Modelos espejo de los DTOs de la API

export interface UsuarioSesion {
  usuarioId: number;
  nombreUsuario: string;
  nombreCompleto: string;
  rol: string;
  sucursalId: number | null;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiraEn: string;
  usuario: UsuarioSesion;
}

export interface VentasIndicador {
  cantidad: number;
  total: number;
  ganancia: number;
}

export interface DashboardDto {
  ventasHoy: VentasIndicador;
  ventasMes: VentasIndicador;
  inventario: { equiposDisponibles: number; accesoriosDisponibles: number; valorCosto: number };
  taller: { recibidos: number; enReparacion: number; reparados: number };
  creditos: {
    activos: number;
    enMora: number;
    saldoPendiente: number;
    cuotasVencidas: number;
    montoVencido: number;
  };
  caja: {
    sesionAbierta: boolean;
    sesionCajaId: number | null;
    montoApertura: number;
    ingresos: number;
    egresos: number;
  };
  topProductosMes: { producto: string; unidades: number; total: number }[];
  topVendedoresMes: { usuarioId: number; vendedor: string; ventas: number; total: number }[];
  ventasUltimos14Dias: { fecha: string; total: number; ganancia: number }[];
}
