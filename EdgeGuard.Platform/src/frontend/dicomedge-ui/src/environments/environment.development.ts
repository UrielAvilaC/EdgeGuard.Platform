import { EnvironmentConfig } from '../app/core/config/environment.config';

export const environment: EnvironmentConfig = {
  production: false,
  apiBaseUrl: 'https://localhost:7228/api',
  signalRUrl: 'https://localhost:7228/hubs/notifications',
};
