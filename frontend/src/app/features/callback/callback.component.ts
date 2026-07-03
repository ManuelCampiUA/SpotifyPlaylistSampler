import { Component, inject } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-callback',
  standalone: true,
  template: `
    <div style="display:flex;align-items:center;justify-content:center;height:100vh;color:#9e9e9e;">
      Accesso in corso...
    </div>
  `,
})
export class CallbackComponent {
  constructor() {
    const route = inject(ActivatedRoute);
    const router = inject(Router);
    const authService = inject(AuthService);

    const token = route.snapshot.queryParamMap.get('token');

    if (token) {
      authService.handleCallback(token);
      router.navigate(['/']);
    } else {
      router.navigate(['/login']);
    }
  }
}
