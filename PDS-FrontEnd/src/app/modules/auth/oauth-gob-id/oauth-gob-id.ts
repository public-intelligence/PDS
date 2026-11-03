import { Component, inject, OnInit, signal } from '@angular/core';
import { AuthService } from '../../../services/auth/auth.service';
import { ActivatedRoute, Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { CredentialsLoaderComponent } from '@public-intelligence/ui-core';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-oauth-gob-id',
  imports: [CredentialsLoaderComponent],
  templateUrl: './oauth-gob-id.html',
})
export class OauthGobId implements OnInit {
  private authService = inject(AuthService);
  private route = inject(ActivatedRoute);
  private code = this.route.snapshot.queryParamMap.get('code');
  private orgAppId =
    this.route.snapshot.queryParamMap.get('orgAppId') ||
    this.route.snapshot.queryParamMap.get('organizationApplicationId') ||
    environment.gobIdApplicationId;
  private router = inject(Router);

  protected isRedirecting = signal(false);

  readonly errorMessage = signal<string | null>(null);
  readonly busy = signal(true);

  ngOnInit(): void {
    if (!this.code) {
      this.redirectToGobIdLogin();
      return;
    }
    this.getToken();
  }

  redirectToGobIdLogin(): void {
    this.authService.redirectToGobIdLogin();
  }

  async getToken() {
    this.isRedirecting.set(true);
    try {
      await firstValueFrom(this.authService.completeGobIdOAuthCallback(this.code!, this.orgAppId!));
      this.router.navigate(['/dashboard']);
    } catch (err) {
      this.errorMessage.set(err instanceof Error ? err.message : 'Error al iniciar sesión');
      this.busy.set(false);
    }
  }
}
