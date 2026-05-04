import { Component, ElementRef, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../services/auth';

interface ChatMessage {
  from: 'user' | 'bot';
  text: string;
  time: string;
}

@Component({
  selector: 'app-chatbot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './chatbot.html',
  styleUrl: './chatbot.scss'
})
export class ChatbotComponent implements OnInit {

  @ViewChild('messagesEnd') messagesEnd!: ElementRef;

  isOpen    = false;
  isTyping  = false;
  userInput = '';
  messages: ChatMessage[] = [];

  private apiUrl = 'http://localhost:5062/api/chatbot/message';

  constructor(private auth: AuthService, private http: HttpClient) {}

  ngOnInit() {
    this.pushBot(
      `👋 Hello, ${this.auth.getUser()?.userFullName ?? 'there'}!\n` +
      `I'm the DSMS Assistant. Type **help** to see what I can do.`
    );
  }

  toggleChat() {
    this.isOpen = !this.isOpen;
    if (this.isOpen) setTimeout(() => this.scrollBottom(), 50);
  }

  sendMessage() {
    const text = this.userInput.trim();
    if (!text || this.isTyping) return;

    this.pushUser(text);
    this.userInput = '';
    this.isTyping  = true;

    this.http.post<{ reply: string }>(
      this.apiUrl,
      { message: text },
      { headers: new HttpHeaders({ Authorization: `Bearer ${this.auth.getToken()}` }) }
    ).subscribe({
      next: (res) => {
        this.isTyping = false;
        this.pushBot(res.reply);
      },
      error: () => {
        this.isTyping = false;
        this.pushBot('⚠️ Sorry, I could not connect to the server. Please try again.');
      }
    });
  }

  onKey(event: KeyboardEvent) {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  clearChat() {
    this.messages = [];
    this.pushBot(`Chat cleared. Type **help** to see what I can do.`);
  }

  private pushUser(text: string) {
    this.messages.push({ from: 'user', text, time: this.now() });
    setTimeout(() => this.scrollBottom(), 30);
  }

  private pushBot(text: string) {
    this.messages.push({ from: 'bot', text, time: this.now() });
    setTimeout(() => this.scrollBottom(), 30);
  }

  private scrollBottom() {
    try { this.messagesEnd.nativeElement.scrollIntoView({ behavior: 'smooth' }); } catch {}
  }

  private now(): string {
    return new Date().toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  }

  /** Converts **bold** markdown and newlines to HTML for display */
  formatText(text: string): string {
    return text
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
      .replace(/\n/g, '<br>');
  }
}
