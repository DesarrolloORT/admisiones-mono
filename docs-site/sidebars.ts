const sidebars = {
  admissionsSidebar: [
    {
      type: 'doc',
      id: 'index',
      label: 'Inicio',
    },
    {
      type: 'doc',
      id: 'ARCHITECTURE',
      label: 'Arquitectura',
    },
    {
      type: 'category',
      label: 'Workflow/Setup',
      items: [
        { type: 'doc', id: 'WORKFLOW', label: 'Workflow' },
        { type: 'doc', id: 'SETUP', label: 'Setup' },
      ],
    },
    {
      type: 'category',
      label: 'Calidad y accesibilidad',
      items: [
        { type: 'doc', id: 'BEST-PRACTICES', label: 'Estandares frontend' },
        { type: 'doc', id: 'ACCESSIBILITY', label: 'Accesibilidad' },
        { type: 'doc', id: 'E2E-GUARDRAILS', label: 'E2E guardrails' },
      ],
    },
    {
      type: 'category',
      label: 'Flujos',
      items: [
        { type: 'doc', id: 'flujos/login', label: 'Login' },
        { type: 'doc', id: 'flujos/registro', label: 'Registro' },
        { type: 'doc', id: 'flujos/inscripciones', label: 'Inscripciones' },
      ],
    },
    {
      type: 'doc',
      id: 'RELEASES',
      label: 'Releases',
    },
    {
      type: 'doc',
      id: 'RUNBOOK',
      label: 'Runbook',
    },
  ],
};

export default sidebars;
