import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { TranslatePipe } from '@ngx-translate/core';
import { LanguageService } from '../../core/language.service';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, TranslatePipe],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss'],
})
export class RegisterComponent {
  name = '';
  email = '';
  role = '';
  password = '';
  confirm = '';
  showPwd = false;
  showConfirm = false;
  agreed = false;

  constructor(
    private readonly router: Router,
    readonly lang: LanguageService
  ) {}

  onRegister(): void {
    void this.router.navigate(['/dashboard']);
  }
}
