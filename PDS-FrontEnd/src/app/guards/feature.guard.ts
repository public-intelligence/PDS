import { inject } from '@angular/core';
import { CanMatchFn, Router } from '@angular/router';
import { FeatureFlag, isFeatureEnabled } from '../utils/feature-flags';

/** Deja la ruta fuera del enrutador mientras la funcionalidad esté apagada en el ambiente. */
export const featureGuard =
  (flag: FeatureFlag): CanMatchFn =>
  () =>
    isFeatureEnabled(flag) || inject(Router).createUrlTree(['/dashboard']);
