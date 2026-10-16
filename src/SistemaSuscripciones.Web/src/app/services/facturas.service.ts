import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  FacturaResumen,
  GuardarFacturaRequest,
  ResultadoRecordatorio,
} from '../models/factura.model';

@Injectable({ providedIn: 'root' })
export class FacturasService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/Facturas';

  obtenerTodas(): Observable<FacturaResumen[]> {
    return this.http.get<FacturaResumen[]>(this.baseUrl);
  }

  crear(solicitud: GuardarFacturaRequest): Observable<FacturaResumen> {
    return this.http.post<FacturaResumen>(this.baseUrl, solicitud);
  }

  actualizar(id: string, solicitud: GuardarFacturaRequest): Observable<FacturaResumen> {
    return this.http.put<FacturaResumen>(`${this.baseUrl}/${encodeURIComponent(id)}`, solicitud);
  }

  eliminar(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${encodeURIComponent(id)}`);
  }

  enviarRecordatorio(id: string): Observable<ResultadoRecordatorio> {
    return this.http.post<ResultadoRecordatorio>(
      `${this.baseUrl}/${encodeURIComponent(id)}/enviar-recordatorio`,
      {},
    );
  }

  procesarPendientes(): Observable<ResultadoRecordatorio[]> {
    return this.http.post<ResultadoRecordatorio[]>(`${this.baseUrl}/procesar-pendientes`, {});
  }
}
