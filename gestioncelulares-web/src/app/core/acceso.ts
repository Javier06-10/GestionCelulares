/**
 * Control de acceso por rol: única fuente de verdad de qué pantallas ve cada rol.
 * La usan tanto el menú lateral como el guard de rutas, para que no haya forma de
 * llegar a una pantalla (ni por menú ni escribiendo la URL) sin el rol adecuado.
 *
 * Clave = path de la ruta (sin barra inicial). Valor = roles permitidos.
 * Si un path no aparece aquí, queda visible para cualquier usuario autenticado.
 */
export const ACCESO: Record<string, string[]> = {
  // General
  '': ['Admin', 'Vendedor', 'Tecnico'],          // Dashboard
  'pos': ['Admin', 'Vendedor'],
  'ventas': ['Admin', 'Vendedor'],               // Historial de ventas
  'devoluciones': ['Admin'],                     // Notas de crédito (revierten ventas)
  'caja': ['Admin', 'Vendedor'],
  'apartados': ['Admin', 'Vendedor'],
  'inventario': ['Admin', 'Vendedor', 'Tecnico'],// Existencias
  'catalogo': ['Admin'],
  'clientes': ['Admin', 'Vendedor'],
  'creditos': ['Admin', 'Vendedor'],
  'garantias': ['Admin', 'Vendedor', 'Tecnico'],
  'taller': ['Admin', 'Vendedor', 'Tecnico'],

  // Administración (solo Admin)
  'proveedores': ['Admin'],
  'nomina': ['Admin'],
  'cuentas': ['Admin'],
  'asientos': ['Admin'],
  'estados': ['Admin'],
  'ncf': ['Admin'],
  'reportes': ['Admin'],
  'usuarios': ['Admin']
};

/** ¿El rol puede ver/entrar al path? Sin restricción configurada = sí. */
export function puedeAcceder(rol: string | undefined | null, path: string): boolean {
  const limpio = (path ?? '').replace(/^\//, '');
  const roles = ACCESO[limpio];
  if (!roles) return true;
  return !!rol && roles.includes(rol);
}
