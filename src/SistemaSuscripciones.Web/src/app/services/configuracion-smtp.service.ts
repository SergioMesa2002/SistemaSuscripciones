import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ConfiguracionSmtp,
  EstadoConfiguracionSmtp,
  ResultadoPruebaSmtp,
} from '../models/configuracion-smtp.model';

@Injectable({ providedIn: 'root' })
export class ConfiguracionSmtpService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/configuracion/smtp';

  obtener(): Observable<EstadoConfiguracionSmtp> {
    return this.http.get<EstadoConfiguracionSmtp>(this.baseUrl);
  }

  actualizar(configuracion: ConfiguracionSmtp): Observable<EstadoConfiguracionSmtp> {
    return this.http.put<EstadoConfiguracionSmtp>(this.baseUrl, configuracion);
  }

  probar(destinatario: string): Observable<ResultadoPruebaSmtp> {
    return this.http.post<ResultadoPruebaSmtp>(`${this.baseUrl}/probar`, { destinatario });
  }
}
