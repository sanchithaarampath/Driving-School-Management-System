import { Component, OnInit } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { AuthService } from './services/auth';
import { ChatbotComponent } from './pages/chatbot/chatbot';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, CommonModule, ChatbotComponent],
  template: `
    <router-outlet></router-outlet>
    <app-chatbot *ngIf="loggedIn"></app-chatbot>
  `,
  styles: []
})
export class App implements OnInit {
  title    = 'DSMS-Frontend';
  loggedIn = false;

  constructor(private auth: AuthService) {}

  ngOnInit() {
    this.loggedIn = this.auth.isLoggedIn();

    // Re-check whenever storage changes (login / logout in another tab)
    window.addEventListener('storage', () => {
      this.loggedIn = this.auth.isLoggedIn();
    });
  }
}
