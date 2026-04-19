import { InjectionToken } from '@angular/core';

export interface EnvironmentConfig {
  production: boolean;
  apiBaseUrl: string;
  signalRUrl: string;
}

export const ENVIRONMENT = new InjectionToken<EnvironmentConfig>('EnvironmentConfig');
