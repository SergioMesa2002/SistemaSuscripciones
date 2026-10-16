import { Injectable } from '@angular/core';
import type { jsPDF } from 'jspdf';
import { EstadoRecordatorio, FacturaResumen } from '../models/factura.model';

@Injectable({ providedIn: 'root' })
export class FacturaPdfService {
  async generar(factura: FacturaResumen): Promise<void> {
    const { jsPDF: ConstructorPdf } = await import('jspdf');
    const documento = new ConstructorPdf({ unit: 'mm', format: 'a4' });
    const verde = [40, 89, 66] as const;
    const verdeOscuro = [24, 60, 43] as const;
    const tinta = [31, 43, 36] as const;
    const gris = [104, 116, 109] as const;
    const fondo = [246, 249, 246] as const;
    const ancho = 210;
    const margen = 18;

    documento.setFillColor(...verdeOscuro);
    documento.rect(0, 0, ancho, 43, 'F');
    documento.setFillColor(220, 239, 200);
    documento.circle(25, 21.5, 8, 'F');
    documento.setTextColor(...verdeOscuro);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(14);
    documento.text('S', 25, 24, { align: 'center' });

    documento.setTextColor(255, 255, 255);
    documento.setFontSize(17);
    documento.text('Sistema Suscripciones', 38, 19);
    documento.setFont('helvetica', 'normal');
    documento.setFontSize(9);
    documento.text('Gestión de membresías y recordatorios', 38, 26);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(22);
    documento.text('FACTURA', 192, 18, { align: 'right' });
    documento.setFontSize(10);
    documento.text(`# ${factura.numeroFactura}`, 192, 26, { align: 'right' });

    this.dibujarDato(
      documento,
      margen,
      54,
      53,
      'FECHA DE EMISIÓN',
      this.formatearFecha(new Date()),
    );
    this.dibujarDato(
      documento,
      76.5,
      54,
      53,
      'VENCIMIENTO',
      this.formatearFecha(new Date(factura.fechaVencimiento)),
    );
    this.dibujarDato(documento, 135, 54, 57, 'ESTADO', this.estadoEtiqueta(factura.estado));

    documento.setTextColor(...gris);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(8);
    documento.text('FACTURAR A', margen, 85);
    documento.setTextColor(...tinta);
    documento.setFontSize(15);
    documento.text(factura.nombreSocio, margen, 94);
    documento.setFont('helvetica', 'normal');
    documento.setFontSize(10);
    documento.setTextColor(...gris);
    documento.text(factura.correoSocio, margen, 101);

    documento.setFillColor(...verde);
    documento.roundedRect(margen, 114, 174, 11, 2, 2, 'F');
    documento.setTextColor(255, 255, 255);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(8);
    documento.text('DESCRIPCIÓN', 22, 121);
    documento.text('CANTIDAD', 139, 121, { align: 'right' });
    documento.text('VALOR', 188, 121, { align: 'right' });

    documento.setFillColor(...fondo);
    documento.roundedRect(margen, 128, 174, 35, 2, 2, 'F');
    documento.setTextColor(...tinta);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(11);
    documento.text(`Plan ${factura.planSuscripcion}`, 22, 139);
    documento.setFont('helvetica', 'normal');
    documento.setTextColor(...gris);
    documento.setFontSize(8.5);
    documento.text(`Suscripción asociada a la factura ${factura.numeroFactura}.`, 22, 147);
    documento.setTextColor(...tinta);
    documento.setFontSize(10);
    documento.text('1', 139, 139, { align: 'right' });
    documento.setFont('helvetica', 'bold');
    documento.text(this.formatearMoneda(factura.valor), 188, 139, { align: 'right' });

    documento.setDrawColor(222, 229, 223);
    documento.line(111, 177, 192, 177);
    documento.setFont('helvetica', 'normal');
    documento.setTextColor(...gris);
    documento.setFontSize(9);
    documento.text('Subtotal', 140, 187);
    documento.setTextColor(...tinta);
    documento.text(this.formatearMoneda(factura.valor), 192, 187, { align: 'right' });
    documento.setFillColor(...verdeOscuro);
    documento.roundedRect(111, 194, 81, 19, 2, 2, 'F');
    documento.setTextColor(255, 255, 255);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(10);
    documento.text('TOTAL', 119, 206);
    documento.setFontSize(14);
    documento.text(this.formatearMoneda(factura.valor), 188, 206, { align: 'right' });

    documento.setFillColor(239, 246, 237);
    documento.roundedRect(margen, 229, 174, 28, 3, 3, 'F');
    documento.setTextColor(...verdeOscuro);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(9);
    documento.text('INFORMACIÓN DEL DOCUMENTO', 23, 239);
    documento.setFont('helvetica', 'normal');
    documento.setTextColor(...gris);
    documento.setFontSize(8);
    const nota =
      'Resumen generado por Sistema Suscripciones. Conserva este documento como soporte administrativo de la obligación registrada.';
    documento.text(documento.splitTextToSize(nota, 160), 23, 247);

    documento.setDrawColor(226, 231, 227);
    documento.line(margen, 277, 192, 277);
    documento.setTextColor(128, 138, 132);
    documento.setFontSize(7.5);
    documento.text('Documento generado electrónicamente por Sistema Suscripciones.', margen, 284);
    documento.text(`ID: ${factura.id}`, 192, 284, { align: 'right' });

    documento.save(`factura-${this.nombreArchivoSeguro(factura.numeroFactura)}.pdf`);
  }

  private dibujarDato(
    documento: jsPDF,
    x: number,
    y: number,
    ancho: number,
    etiqueta: string,
    valor: string,
  ): void {
    documento.setFillColor(246, 249, 246);
    documento.roundedRect(x, y, ancho, 18, 2, 2, 'F');
    documento.setTextColor(112, 123, 117);
    documento.setFont('helvetica', 'bold');
    documento.setFontSize(7);
    documento.text(etiqueta, x + 5, y + 6);
    documento.setTextColor(31, 43, 36);
    documento.setFontSize(9);
    documento.text(valor, x + 5, y + 13);
  }

  private formatearMoneda(valor: number): string {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      maximumFractionDigits: 0,
    }).format(valor);
  }

  private formatearFecha(fecha: Date): string {
    return new Intl.DateTimeFormat('es-CO', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(fecha);
  }

  private estadoEtiqueta(estado: EstadoRecordatorio): string {
    const etiquetas: Record<EstadoRecordatorio, string> = {
      PrimerRecordatorio: 'Primer recordatorio',
      SegundoRecordatorio: 'Segundo recordatorio',
      Desactivado: 'Desactivado',
    };

    return etiquetas[estado];
  }

  private nombreArchivoSeguro(valor: string): string {
    return valor.replace(/[^a-zA-Z0-9_-]+/g, '-').replace(/^-|-$/g, '') || 'documento';
  }
}
