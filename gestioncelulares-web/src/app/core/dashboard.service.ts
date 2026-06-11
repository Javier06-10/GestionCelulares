import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { DashboardDto } from './models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  obtener(sucursalId?: number) {
    let params = new HttpParams();
    if (sucursalId) {
      params = params.set('sucursalId', sucursalId);
    }
    return this.http.get<DashboardDto>(`${environment.apiUrl}/dashboard`, { params });
  }
}
