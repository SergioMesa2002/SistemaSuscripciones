import { Component, HostListener, OnInit, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { ConfiguracionSmtp } from './models/configuracion-smtp.model';
import { EstadoRecordatorio, FacturaResumen, GuardarFacturaRequest } from './models/factura.model';
import { ConfiguracionSmtpService } from './services/configuracion-smtp.service';
import { FacturaPdfService } from './services/factura-pdf.service';
import { FacturasService } from './services/facturas.service';

type TipoMensaje = 'exito' | 'error' | 'info';

interface MensajeUi {
  tipo: TipoMensaje;
  texto: string;
}

@Component({
  imports: [ReactiveFormsModule],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
})
export class App implements OnInit {
  private readonly facturasService = inject(FacturasService);
  private readonly configuracionSmtpService = inject(ConfiguracionSmtpService);
  private readonly facturaPdfService = inject(FacturaPdfService);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly facturas = signal<FacturaResumen[]>([]);
  protected readonly cargando = signal(true);
  protected readonly guardando = signal(false);
  protected readonly procesandoPendientes = signal(false);
  protected readonly accionId = signal<string | null>(null);
  protected readonly filtro = signal('');
  protected readonly modalAbierto = signal(false);
  protected readonly facturaEditando = signal<FacturaResumen | null>(null);
  protected readonly facturaVistaPrevia = signal<FacturaResumen | null>(null);
  protected readonly mensaje = signal<MensajeUi | null>(null);
  protected readonly vista = signal<'inicio' | 'facturas' | 'configuracion'>('inicio');
  protected readonly menuAbierto = signal(false);
  protected readonly cargandoSmtp = signal(false);
  protected readonly guardandoSmtp = signal(false);
  protected readonly probandoSmtp = signal(false);
  protected readonly generandoPdf = signal(false);
  protected readonly smtpConfigurada = signal(false);
  protected readonly contrasenaConfigurada = signal(false);

  protected readonly formulario = this.formBuilder.nonNullable.group({
    numeroFactura: ['', [Validators.required, Validators.maxLength(50)]],
    nombreSocio: ['', [Validators.required, Validators.maxLength(120)]],
    correoSocio: ['', [Validators.required, Validators.email, Validators.maxLength(160)]],
    planSuscripcion: ['', [Validators.required, Validators.maxLength(100)]],
    valor: [0, [Validators.required, Validators.min(0.01), Validators.max(999999999999.99)]],
    fechaVencimiento: [this.fechaPredeterminada(), Validators.required],
    estado: ['PrimerRecordatorio' as EstadoRecordatorio, Validators.required],
  });

  protected readonly formularioSmtp = this.formBuilder.nonNullable.group({
    host: ['smtp.gmail.com', Validators.required],
    puerto: [587, [Validators.required, Validators.min(1), Validators.max(65535)]],
    usuario: ['', [Validators.required, Validators.email]],
    contrasena: [''],
    remitente: ['', [Validators.required, Validators.email]],
    habilitarSsl: [true],
    destinatario: ['', Validators.email],
    asuntoPrimerRecordatorio: [
      'Recordatorio de pago - Factura {NumeroFactura}',
      [Validators.required, Validators.maxLength(200)],
    ],
    mensajePrimerRecordatorio: [
      'Hola {NombreSocio},\n\nEste es un primer recordatorio del pago pendiente por {Valor}, correspondiente a la factura {NumeroFactura}, con vencimiento el {FechaVencimiento}.\n\nPor favor, realiza el pago a la mayor brevedad posible.',
      [Validators.required, Validators.maxLength(4000)],
    ],
    asuntoSegundoRecordatorio: [
      'Último recordatorio de pago - Factura {NumeroFactura}',
      [Validators.required, Validators.maxLength(200)],
    ],
    mensajeSegundoRecordatorio: [
      'Hola {NombreSocio},\n\nEste es el último aviso del pago pendiente por {Valor}, correspondiente a la factura {NumeroFactura}.\n\nSi el pago no se regulariza, los recordatorios de esta factura serán desactivados.',
      [Validators.required, Validators.maxLength(4000)],
    ],
  });

  protected readonly facturasFiltradas = computed(() => {
    const termino = this.normalizar(this.filtro());

    if (!termino) {
      return this.facturas();
    }

    return this.facturas().filter((factura) =>
      [
        factura.numeroFactura,
        factura.nombreSocio,
        factura.correoSocio,
        factura.planSuscripcion,
        this.estadoEtiqueta(factura.estado),
      ].some((valor) => this.normalizar(valor).includes(termino)),
    );
  });

  protected readonly totalFacturas = computed(() => this.facturas().length);
  protected readonly facturasActivas = computed(
    () => this.facturas().filter((factura) => factura.estado !== 'Desactivado').length,
  );
  protected readonly facturasVencidas = computed(
    () => this.facturas().filter((factura) => this.estaVencida(factura)).length,
  );
  protected readonly valorPendiente = computed(() =>
    this.facturas().reduce((total, factura) => total + factura.valor, 0),
  );

  ngOnInit(): void {
    void this.cargarFacturas();
  }

  protected async cargarFacturas(): Promise<void> {
    this.cargando.set(true);

    try {
      const facturas = await firstValueFrom(this.facturasService.obtenerTodas());
      this.facturas.set(facturas);
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.cargando.set(false);
    }
  }

  protected actualizarFiltro(event: Event): void {
    this.filtro.set((event.target as HTMLInputElement).value);
  }

  protected alternarMenu(): void {
    this.menuAbierto.update((abierto) => !abierto);
  }

  protected cerrarMenu(): void {
    this.menuAbierto.set(false);
  }

  @HostListener('document:keydown.escape')
  protected cerrarMenuConEscape(): void {
    this.cerrarMenu();
  }

  protected mostrarInicio(
    seccion?: 'juridica' | 'tecnologica' | 'contable' | 'noticias' | 'contacto',
  ): void {
    this.cerrarMenu();
    this.vista.set('inicio');

    if (seccion) {
      window.setTimeout(() =>
        document.getElementById(seccion)?.scrollIntoView({ behavior: 'smooth' }),
      );
    }
  }

  protected mostrarFacturas(): void {
    this.cerrarMenu();
    this.vista.set('facturas');
  }

  protected mostrarConfiguracion(): void {
    this.cerrarMenu();
    this.vista.set('configuracion');
    void this.cargarConfiguracionSmtp();
  }

  protected async cargarConfiguracionSmtp(): Promise<void> {
    this.cargandoSmtp.set(true);

    try {
      const estado = await firstValueFrom(this.configuracionSmtpService.obtener());
      this.smtpConfigurada.set(estado.configurada);
      this.contrasenaConfigurada.set(estado.contrasenaConfigurada);
      this.formularioSmtp.patchValue({
        host: estado.host || 'smtp.gmail.com',
        puerto: estado.puerto || 587,
        usuario: estado.usuario,
        contrasena: '',
        remitente: estado.remitente,
        habilitarSsl: estado.habilitarSsl,
        destinatario: this.formularioSmtp.controls.destinatario.value || estado.usuario,
        asuntoPrimerRecordatorio: estado.asuntoPrimerRecordatorio,
        mensajePrimerRecordatorio: estado.mensajePrimerRecordatorio,
        asuntoSegundoRecordatorio: estado.asuntoSegundoRecordatorio,
        mensajeSegundoRecordatorio: estado.mensajeSegundoRecordatorio,
      });
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.cargandoSmtp.set(false);
    }
  }

  protected async guardarConfiguracionSmtp(): Promise<void> {
    const controles = this.formularioSmtp.controls;
    const configuracionInvalida = [
      controles.host,
      controles.puerto,
      controles.usuario,
      controles.remitente,
      controles.asuntoPrimerRecordatorio,
      controles.mensajePrimerRecordatorio,
      controles.asuntoSegundoRecordatorio,
      controles.mensajeSegundoRecordatorio,
    ].some((control) => control.invalid);

    if (configuracionInvalida) {
      this.formularioSmtp.markAllAsTouched();
      this.notificar('error', 'Revisa los datos SMTP y los mensajes antes de guardar.');
      return;
    }

    if (!controles.contrasena.value && !this.contrasenaConfigurada()) {
      controles.contrasena.setErrors({ required: true });
      controles.contrasena.markAsTouched();
      this.notificar('error', 'Ingresa la contraseña de aplicación.');
      return;
    }

    const valores = this.formularioSmtp.getRawValue();
    const configuracion: ConfiguracionSmtp = {
      host: valores.host,
      puerto: Number(valores.puerto),
      usuario: valores.usuario,
      contrasena: valores.contrasena,
      remitente: valores.remitente,
      habilitarSsl: valores.habilitarSsl,
      asuntoPrimerRecordatorio: valores.asuntoPrimerRecordatorio,
      mensajePrimerRecordatorio: valores.mensajePrimerRecordatorio,
      asuntoSegundoRecordatorio: valores.asuntoSegundoRecordatorio,
      mensajeSegundoRecordatorio: valores.mensajeSegundoRecordatorio,
    };
    this.guardandoSmtp.set(true);

    try {
      const estado = await firstValueFrom(this.configuracionSmtpService.actualizar(configuracion));
      this.smtpConfigurada.set(estado.configurada);
      this.contrasenaConfigurada.set(estado.contrasenaConfigurada);
      controles.contrasena.reset('');
      this.notificar('exito', 'La configuración SMTP y los mensajes se guardaron correctamente.');
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.guardandoSmtp.set(false);
    }
  }

  protected async probarConfiguracionSmtp(): Promise<void> {
    const destinatario = this.formularioSmtp.controls.destinatario;
    if (!destinatario.value || destinatario.invalid) {
      destinatario.setErrors({ email: true });
      destinatario.markAsTouched();
      this.notificar('error', 'Ingresa un destinatario válido para la prueba.');
      return;
    }

    this.probandoSmtp.set(true);

    try {
      const resultado = await firstValueFrom(
        this.configuracionSmtpService.probar(destinatario.value),
      );
      this.notificar(resultado.exito ? 'exito' : 'error', resultado.mensaje);
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.probandoSmtp.set(false);
    }
  }

  protected abrirCreacion(): void {
    this.cerrarMenu();
    this.facturaEditando.set(null);
    this.formulario.reset({
      numeroFactura: '',
      nombreSocio: '',
      correoSocio: '',
      planSuscripcion: '',
      valor: 0,
      fechaVencimiento: this.fechaPredeterminada(),
      estado: 'PrimerRecordatorio',
    });
    this.modalAbierto.set(true);
  }

  protected abrirEdicion(factura: FacturaResumen): void {
    this.facturaEditando.set(factura);
    this.formulario.reset({
      numeroFactura: factura.numeroFactura,
      nombreSocio: factura.nombreSocio,
      correoSocio: factura.correoSocio,
      planSuscripcion: factura.planSuscripcion,
      valor: factura.valor,
      fechaVencimiento: factura.fechaVencimiento.slice(0, 10),
      estado: factura.estado,
    });
    this.modalAbierto.set(true);
  }

  protected abrirVistaPrevia(factura: FacturaResumen): void {
    this.facturaVistaPrevia.set(factura);
  }

  protected cerrarVistaPrevia(): void {
    this.facturaVistaPrevia.set(null);
  }

  protected async descargarPdf(factura: FacturaResumen): Promise<void> {
    this.generandoPdf.set(true);

    try {
      await this.facturaPdfService.generar(factura);
      this.notificar('exito', `Se generó el PDF de la factura ${factura.numeroFactura}.`);
    } catch {
      this.notificar('error', 'No fue posible generar el PDF. Intenta nuevamente.');
    } finally {
      this.generandoPdf.set(false);
    }
  }

  protected cerrarModal(): void {
    if (!this.guardando()) {
      this.modalAbierto.set(false);
    }
  }

  protected async guardarFactura(): Promise<void> {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      this.notificar('error', 'Revisa los campos marcados antes de guardar.');
      return;
    }

    const valores = this.formulario.getRawValue();
    const solicitud: GuardarFacturaRequest = {
      ...valores,
      valor: Number(valores.valor),
      fechaVencimiento: `${valores.fechaVencimiento}T00:00:00.000Z`,
    };
    const facturaActual = this.facturaEditando();
    this.guardando.set(true);

    try {
      const guardada = facturaActual
        ? await firstValueFrom(this.facturasService.actualizar(facturaActual.id, solicitud))
        : await firstValueFrom(this.facturasService.crear(solicitud));

      this.facturas.update((facturas) =>
        facturaActual
          ? facturas.map((factura) => (factura.id === guardada.id ? guardada : factura))
          : [guardada, ...facturas],
      );
      this.modalAbierto.set(false);
      this.notificar(
        'exito',
        facturaActual ? 'La factura se actualizó correctamente.' : 'La factura fue creada.',
      );
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.guardando.set(false);
    }
  }

  protected async eliminarFactura(factura: FacturaResumen): Promise<void> {
    const confirmada = window.confirm(
      `¿Deseas eliminar definitivamente la factura ${factura.numeroFactura}?`,
    );

    if (!confirmada) {
      return;
    }

    this.accionId.set(factura.id);

    try {
      await firstValueFrom(this.facturasService.eliminar(factura.id));
      this.facturas.update((facturas) => facturas.filter((item) => item.id !== factura.id));
      this.notificar('exito', `La factura ${factura.numeroFactura} fue eliminada.`);
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.accionId.set(null);
    }
  }

  protected async enviarRecordatorio(factura: FacturaResumen): Promise<void> {
    this.accionId.set(factura.id);

    try {
      const resultado = await firstValueFrom(this.facturasService.enviarRecordatorio(factura.id));
      await this.cargarFacturas();
      this.notificar(resultado.exito ? 'exito' : 'error', resultado.mensaje);
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.accionId.set(null);
    }
  }

  protected async procesarTodos(): Promise<void> {
    if (this.facturasActivas() === 0) {
      this.notificar('info', 'No hay facturas con recordatorios pendientes.');
      return;
    }

    this.procesandoPendientes.set(true);

    try {
      const resultados = await firstValueFrom(this.facturasService.procesarPendientes());
      await this.cargarFacturas();
      this.notificar(
        'exito',
        `Se procesaron ${resultados.length} recordatorio${resultados.length === 1 ? '' : 's'}.`,
      );
    } catch (error) {
      this.notificar('error', this.obtenerMensajeError(error));
    } finally {
      this.procesandoPendientes.set(false);
    }
  }

  protected estadoEtiqueta(estado: EstadoRecordatorio): string {
    const etiquetas: Record<EstadoRecordatorio, string> = {
      PrimerRecordatorio: 'Primer recordatorio',
      SegundoRecordatorio: 'Segundo recordatorio',
      Desactivado: 'Desactivado',
    };

    return etiquetas[estado];
  }

  protected estadoClase(estado: EstadoRecordatorio): string {
    return `estado estado--${estado.toLowerCase()}`;
  }

  protected formatearMoneda(valor: number): string {
    return new Intl.NumberFormat('es-CO', {
      style: 'currency',
      currency: 'COP',
      maximumFractionDigits: 0,
    }).format(valor);
  }

  protected formatearFecha(fecha: string | null): string {
    if (!fecha) {
      return 'Sin envíos';
    }

    return new Intl.DateTimeFormat('es-CO', {
      day: '2-digit',
      month: 'short',
      year: 'numeric',
    }).format(new Date(fecha));
  }

  protected estaVencida(factura: FacturaResumen): boolean {
    const hoy = new Date();
    hoy.setHours(0, 0, 0, 0);
    return new Date(factura.fechaVencimiento).getTime() < hoy.getTime();
  }

  protected limpiarMensaje(): void {
    this.mensaje.set(null);
  }

  private fechaPredeterminada(): string {
    const fecha = new Date();
    fecha.setDate(fecha.getDate() + 30);
    return fecha.toISOString().slice(0, 10);
  }

  private normalizar(valor: string): string {
    return valor
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '')
      .toLowerCase()
      .trim();
  }

  private notificar(tipo: TipoMensaje, texto: string): void {
    const mensaje = { tipo, texto } satisfies MensajeUi;
    this.mensaje.set(mensaje);

    window.setTimeout(() => {
      if (this.mensaje() === mensaje) {
        this.mensaje.set(null);
      }
    }, 5000);
  }

  private obtenerMensajeError(error: unknown): string {
    if (!(error instanceof HttpErrorResponse)) {
      return 'Ocurrió un error inesperado. Intenta nuevamente.';
    }

    if (error.status === 0) {
      return 'No fue posible conectar con la API. Verifica que el backend esté en ejecución.';
    }

    if (error.error && typeof error.error === 'object') {
      const detalle = error.error as {
        detail?: string;
        mensaje?: string;
        errors?: Record<string, string[]>;
      };

      if (detalle.mensaje) {
        return detalle.mensaje;
      }

      if (detalle.detail) {
        return detalle.detail;
      }

      const primerError = detalle.errors ? Object.values(detalle.errors).flat()[0] : undefined;
      if (primerError) {
        return primerError;
      }
    }

    return 'La operación no pudo completarse. Revisa los datos e intenta nuevamente.';
  }
}
