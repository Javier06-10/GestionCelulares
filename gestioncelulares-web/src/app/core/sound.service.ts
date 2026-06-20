import { Injectable, signal } from '@angular/core';

const MUTED_KEY = 'gc_sound_muted';

@Injectable({ providedIn: 'root' })
export class SoundService {
  readonly muted = signal<boolean>(this.leerMuted());

  private audioCtx: AudioContext | null = null;

  toggleMute(): void {
    const nuevoEstado = !this.muted();
    localStorage.setItem(MUTED_KEY, nuevoEstado ? 'true' : 'false');
    this.muted.set(nuevoEstado);
  }

  private leerMuted(): boolean {
    return localStorage.getItem(MUTED_KEY) === 'true';
  }

  private getAudioContext(): AudioContext {
    if (!this.audioCtx) {
      this.audioCtx = new (window.AudioContext || (window as any).webkitAudioContext)();
    }
    if (this.audioCtx.state === 'suspended') {
      this.audioCtx.resume();
    }
    return this.audioCtx;
  }

  playScan(): void {
    if (this.muted()) return;
    try {
      const ctx = this.getAudioContext();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();

      osc.connect(gain);
      gain.connect(ctx.destination);

      osc.type = 'sine';
      osc.frequency.setValueAtTime(950, ctx.currentTime);
      gain.gain.setValueAtTime(0.08, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.08);

      osc.start();
      osc.stop(ctx.currentTime + 0.08);
    } catch (e) {
      console.warn('Web Audio error:', e);
    }
  }

  playSuccess(): void {
    if (this.muted()) return;
    try {
      const ctx = this.getAudioContext();
      
      // Tono 1: Campana de caja registradora
      const osc1 = ctx.createOscillator();
      const gain1 = ctx.createGain();
      osc1.connect(gain1);
      gain1.connect(ctx.destination);
      osc1.type = 'sine';
      osc1.frequency.setValueAtTime(587.33, ctx.currentTime); // D5
      gain1.gain.setValueAtTime(0.12, ctx.currentTime);
      gain1.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.35);
      osc1.start();
      osc1.stop(ctx.currentTime + 0.35);

      // Tono 2: Retardo corto de metal
      const osc2 = ctx.createOscillator();
      const gain2 = ctx.createGain();
      osc2.connect(gain2);
      gain2.connect(ctx.destination);
      osc2.type = 'sine';
      osc2.frequency.setValueAtTime(1174.66, ctx.currentTime + 0.08); // D6
      gain2.gain.setValueAtTime(0.08, ctx.currentTime + 0.08);
      gain2.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.08 + 0.4);
      osc2.start(ctx.currentTime + 0.08);
      osc2.stop(ctx.currentTime + 0.08 + 0.4);

    } catch (e) {
      console.warn('Web Audio error:', e);
    }
  }

  playError(): void {
    if (this.muted()) return;
    try {
      const ctx = this.getAudioContext();
      const osc1 = ctx.createOscillator();
      const osc2 = ctx.createOscillator();
      const gain = ctx.createGain();

      osc1.connect(gain);
      osc2.connect(gain);
      gain.connect(ctx.destination);

      osc1.type = 'sawtooth';
      osc2.type = 'sine';

      osc1.frequency.setValueAtTime(130, ctx.currentTime);
      osc2.frequency.setValueAtTime(135, ctx.currentTime); // Disonancia sutil

      gain.gain.setValueAtTime(0.1, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.28);

      osc1.start();
      osc2.start();
      osc1.stop(ctx.currentTime + 0.28);
      osc2.stop(ctx.currentTime + 0.28);
    } catch (e) {
      console.warn('Web Audio error:', e);
    }
  }

  playSlide(): void {
    if (this.muted()) return;
    try {
      const ctx = this.getAudioContext();
      const osc = ctx.createOscillator();
      const gain = ctx.createGain();

      osc.connect(gain);
      gain.connect(ctx.destination);

      osc.type = 'sine';
      osc.frequency.setValueAtTime(320, ctx.currentTime);
      osc.frequency.exponentialRampToValueAtTime(540, ctx.currentTime + 0.15);

      gain.gain.setValueAtTime(0.06, ctx.currentTime);
      gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.15);

      osc.start();
      osc.stop(ctx.currentTime + 0.15);
    } catch (e) {
      console.warn('Web Audio error:', e);
    }
  }
}
