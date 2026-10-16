export type EstadoRecordatorio = 'PrimerRecordatorio' | 'SegundoRecordatorio' | 'Desactivado';

export interface FacturaResumen {
  id: string;
  numeroFactura: string;
  nombreSocio: string;
  correoSocio: string;
  planSuscripcion: string;
  valor: number;
  fechaVencimiento: string;
  estado: EstadoRecordatorio;
  ultimoCorreoEnviado: string | null;
}

export interface GuardarFacturaRequest {
  numeroFactura: string;
  nombreSocio: string;
  correoSocio: string;
  planSuscripcion: string;
  valor: number;
  fechaVencimiento: string;
  estado: EstadoRecordatorio;
}

export interface ResultadoRecordatorio {
  exito: boolean;
  mensaje: string;
  facturaId: string;
  nuevoEstado: EstadoRecordatorio | null;
}
