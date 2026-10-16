import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { App } from './app';

describe('App', () => {
  let httpTesting: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('crea la aplicación y consulta las facturas', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    httpTesting.expectOne('/api/Facturas').flush([]);
    await fixture.whenStable();

    expect(fixture.componentInstance).toBeTruthy();
  });

  it('muestra el título principal del panel', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    httpTesting.expectOne('/api/Facturas').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const pagina = fixture.nativeElement as HTMLElement;
    expect(pagina.querySelector('h1')?.textContent).toContain('Gestión de facturas');
  });

  it('abre la configuración y consulta el estado SMTP sin recibir la contraseña', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    httpTesting.expectOne('/api/Facturas').flush([]);
    await fixture.whenStable();
    fixture.detectChanges();

    const pagina = fixture.nativeElement as HTMLElement;
    const botonConfiguracion = [
      ...pagina.querySelectorAll<HTMLButtonElement>('.navigation__item'),
    ].find((boton) => boton.textContent?.includes('Configuración'));

    botonConfiguracion?.click();
    fixture.detectChanges();
    httpTesting.expectOne('/api/configuracion/smtp').flush({
      host: 'smtp.gmail.com',
      puerto: 587,
      usuario: 'prueba@example.com',
      remitente: 'prueba@example.com',
      habilitarSsl: true,
      contrasenaConfigurada: true,
      configurada: true,
      asuntoPrimerRecordatorio: 'Primer aviso {NumeroFactura}',
      mensajePrimerRecordatorio: 'Hola {NombreSocio}',
      asuntoSegundoRecordatorio: 'Último aviso {NumeroFactura}',
      mensajeSegundoRecordatorio: 'Último mensaje para {NombreSocio}',
    });
    await fixture.whenStable();
    fixture.detectChanges();

    expect(pagina.querySelector('h1')?.textContent).toContain('Configuración SMTP');
    expect(pagina.querySelector<HTMLInputElement>('input[type="password"]')?.value).toBe('');
  });

  it('abre la vista previa de una factura desde la tabla', async () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    httpTesting.expectOne('/api/Facturas').flush([
      {
        id: 'factura-1',
        numeroFactura: 'SUS-100',
        nombreSocio: 'Juan Pérez',
        correoSocio: 'juan@example.com',
        planSuscripcion: 'Premium',
        valor: 185000,
        fechaVencimiento: '2026-11-30T00:00:00Z',
        estado: 'PrimerRecordatorio',
        ultimoCorreoEnviado: null,
      },
    ]);
    await fixture.whenStable();
    fixture.detectChanges();

    const pagina = fixture.nativeElement as HTMLElement;
    pagina.querySelector<HTMLButtonElement>('.invoice-link')?.click();
    fixture.detectChanges();

    expect(pagina.querySelector('.invoice-modal')).toBeTruthy();
    expect(pagina.querySelector('.invoice-preview__number')?.textContent).toContain('SUS-100');
    expect(pagina.querySelector('.invoice-modal__actions')?.textContent).toContain('Descargar PDF');
  });
});
