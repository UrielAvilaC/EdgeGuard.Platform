import { EnvironmentConfig } from '../app/core/config/environment.config';

export const environment: EnvironmentConfig = {
  production: false,
  apiBaseUrl: 'http://localhost:5197/api',
  signalRUrl: 'http://localhost:5197/hubs/notifications',
};
