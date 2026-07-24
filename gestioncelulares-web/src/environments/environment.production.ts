// Entorno de PRODUCCIÓN. Reemplaza a environment.ts en el build `production`
// (ver fileReplacements en angular.json).
//
// apiUrl relativo (/api): el frontend llama al mismo origen desde el que se sirve,
// y un reverse proxy (IIS/nginx) enruta /api hacia la API .NET. Así no se hardcodea
// ningún dominio. Si la API vive en otro host, cambia esta línea por la URL absoluta,
// p. ej. 'https://api.tu-dominio.com/api'.
export const environment = {
  production: true,
  apiUrl: '/api',
};
