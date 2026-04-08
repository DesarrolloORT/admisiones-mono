import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-landing',
  imports: [MatIconModule],
  templateUrl: './landing.html',
  styleUrl: './landing.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Landing {
  protected readonly currentYear = signal(new Date().getFullYear());

  protected readonly features = signal([
    {
      title: 'Bibliotecas Integradas',
      description:
        'Incluye Angular, Angular Material, ng-recaptcha, rxjs, @desarrolloort/design-system y @desarrolloort/ngx-utils, entre otras.',
      icon: 'package_2',
    },
    {
      title: 'Swagger Codegen',
      description: 'Genera automáticamente las interfaces de los modelos de la API REST.',
      icon: 'modeling',
    },
    {
      title: 'Loader Compartido',
      description: 'Incluye un loader reutilizable para feedback visual durante las peticiones.',
      icon: 'progress_activity',
    },
    {
      title: 'Servicio de Caché',
      description: 'Optimiza las peticiones a la API REST mediante un servicio de caché integrado.',
      icon: 'save',
    },
    {
      title: 'Estructura por Feature',
      description:
        'Organización del código por dominio con `core`, `shared` y features lazy-loaded.',
      icon: 'dashboard',
    },
    {
      title: 'Contenedor de desarrollo',
      description:
        'Implementación de un Dev Container para un entorno de desarrollo preconfigurado con Angular y Node.',
      icon: 'deployed_code',
    },
  ]);

  protected readonly tourSteps = signal([
    {
      title: 'Instalación',
      description:
        'Clona el repositorio y ejecuta `npm install` para instalar las dependencias (si estás viendo esto, probablemente este paso ya lo hayas hecho).',
      code: 'git clone https://github.com/DesarrolloORT/angular-template.git\ncd angular-template\nnpm install',
    },
    {
      title: 'Renombrar el Proyecto',
      description:
        'Reemplaza todas las instancias de "angular-template" por el nombre de tu proyecto en minúsculas, usando guiones en lugar de espacios (si los tiene).',
      code: 'CTRL + SHIFT + H\nBuscar: angular-template\nReemplazar: mi-nuevo-proyecto',
    },
    {
      title: 'Configuración de Ambientes',
      description:
        'Renombra y crea los archivos de ambiente necesarios: environment.ts, environment.prod.ts, environment.staging.ts y environment.dev.ts.',
      code: 'mv environment.template.ts environment.ts\nmv environment.prod.template.ts environment.prod.ts',
    },
    {
      title: 'Servidor de Desarrollo',
      description: 'Inicia el servidor de desarrollo y abre la aplicación en el navegador.',
      code: 'ng serve',
    },
  ]);
}
