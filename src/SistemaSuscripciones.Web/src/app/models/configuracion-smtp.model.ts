export interface ConfiguracionSmtp {
  host: string;
  puerto: number;
  usuario: string;
  contrasena: string;
  remitente: string;
  habilitarSsl: boolean;
  asuntoPrimerRecordatorio: string;
  mensajePrimerRecordatorio: string;
  asuntoSegundoRecordatorio: string;
  mensajeSegundoRecordatorio: string;
}

export interface EstadoConfiguracionSmtp {
  host: string;
  puerto: number;
  usuario: string;
  remitente: string;
  habilitarSsl: boolean;
  contrasenaConfigurada: boolean;
  configurada: boolean;
  asuntoPrimerRecordatorio: string;
  mensajePrimerRecordatorio: string;
  asuntoSegundoRecordatorio: string;
  mensajeSegundoRecordatorio: string;
}

export interface ResultadoPruebaSmtp {
  exito: boolean;
  mensaje: string;
}
