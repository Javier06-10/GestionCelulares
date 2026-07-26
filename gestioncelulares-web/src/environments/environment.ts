export const environment = {
  production: false,
  apiUrl: 'http://localhost:5289/api',
  // Subida de imágenes de producto a Cloudinary (unsigned upload). Valores PÚBLICOS
  // (no secretos): crea una cuenta gratis en cloudinary.com, un "unsigned upload preset"
  // y pega aquí el cloud name y el nombre del preset. Vacío = subir imágenes deshabilitado.
  cloudinary: {
    cloudName: '',
    uploadPreset: '',
  },
};
