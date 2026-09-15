import { createConvention } from '@desarrolloort/azure-env-sync/src/convention.mjs';

const config = createConvention('admisiones');

export default {
  ...config,
  settings: options =>
    config
      .settings(options)
      .map(setting =>
        setting.name === 'fdpApiUrl'
          ? { ...setting, label: process.env.FDP_API_LABEL ?? options.env }
          : setting
      ),
};
