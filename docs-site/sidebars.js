const sidebars = {
  admissionsSidebar: [
    {
      type: 'doc',
      id: 'ADMISSIONS',
      label: 'Introducción',
    },
    {
      type: 'doc',
      id: 'CONFIGURACION_AMBIENTE_AZURE',
      label: 'Frontend local con Azure',
    },
    {
      type: 'category',
      label: 'Flujos',
      items: ['LOGIN-FLOW', 'REGISTER-FLOW'],
    },
    {
      type: 'category',
      label: 'Arquitectura',
      items: ['ARCHITECTURE', 'BEST-PRACTICES', 'DOCUMENTATION-GUIDELINES'],
    },
  ],
};

export default sidebars;
