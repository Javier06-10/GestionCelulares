import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, map, throwError } from 'rxjs';
import { environment } from '../../environments/environment';

/**
 * Sube imágenes a un store externo (Cloudinary, unsigned upload) directamente desde el
 * navegador y devuelve el URL público. Nuestra API solo persiste ese URL — los bytes no
 * pasan por nuestro backend. Para cambiar de proveedor, solo se reescribe `subir()`.
 */
@Injectable({ providedIn: 'root' })
export class ImagenService {
  private http = inject(HttpClient);

  /** true si Cloudinary está configurado (cloudName + uploadPreset en environment). */
  get habilitado(): boolean {
    const c = environment.cloudinary;
    return !!(c && c.cloudName && c.uploadPreset);
  }

  /** Sube el archivo y emite el `secure_url` de la imagen. */
  subir(archivo: File): Observable<string> {
    const c = environment.cloudinary;
    if (!c?.cloudName || !c?.uploadPreset) {
      return throwError(() => new Error('La subida de imágenes no está configurada (environment.cloudinary).'));
    }

    const form = new FormData();
    form.append('file', archivo);
    form.append('upload_preset', c.uploadPreset);

    // URL de Cloudinary (no es nuestra API): el interceptor de auth no la toca.
    return this.http
      .post<{ secure_url: string }>(`https://api.cloudinary.com/v1_1/${c.cloudName}/image/upload`, form)
      .pipe(map(r => r.secure_url));
  }
}
