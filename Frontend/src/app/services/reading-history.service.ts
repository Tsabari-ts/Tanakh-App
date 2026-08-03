import { Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'tanach.readHistory.v1';

/** book|chapter -> ISO date string the chapter was marked read. */
type ReadMap = Record<string, string>;

function loadReadMap(): ReadMap {
  try {
    return JSON.parse(localStorage.getItem(STORAGE_KEY) || '{}');
  } catch {
    return {};
  }
}

function key(book: string, chapter: number): string {
  return `${book}|${chapter}`;
}

/**
 * Purely client-side reading-history tracker (which chapters the user has
 * marked read, and on what date) - powers the home screen's read-count/streak
 * stats and the chapter grid's read checkmarks. No backend endpoint exists
 * for this today, so it lives entirely in localStorage; it does not replace
 * or touch the existing `HasStorage`/`SectionRef` "last position" mechanism
 * or the `updateReadingProgress` API call.
 */
@Injectable({ providedIn: 'root' })
export class ReadingHistoryService {
  private readonly _read = signal<ReadMap>(loadReadMap());
  readonly read = this._read.asReadonly();

  isRead(book: string, chapter: number): boolean {
    return key(book, chapter) in this._read();
  }

  markRead(book: string, chapter: number): void {
    this._read.update(map => ({ ...map, [key(book, chapter)]: new Date().toISOString() }));
    this.persist();
  }

  unmarkRead(book: string, chapter: number): void {
    this._read.update(map => {
      const { [key(book, chapter)]: _, ...rest } = map;
      return rest;
    });
    this.persist();
  }

  toggleRead(book: string, chapter: number): void {
    if (this.isRead(book, chapter)) {
      this.unmarkRead(book, chapter);
    } else {
      this.markRead(book, chapter);
    }
  }

  countForBook(book: string): number {
    const prefix = `${book}|`;
    return Object.keys(this._read()).filter(k => k.startsWith(prefix)).length;
  }

  totalRead(): number {
    return Object.keys(this._read()).length;
  }

  /** Consecutive-day streak ending today or yesterday (0 if the streak was broken). */
  streak(): number {
    const days = new Set(Object.values(this._read()).map(iso => iso.slice(0, 10)));
    if (days.size === 0) return 0;

    let streak = 0;
    const cursor = new Date();
    cursor.setHours(0, 0, 0, 0);

    if (!days.has(cursor.toISOString().slice(0, 10))) {
      cursor.setDate(cursor.getDate() - 1);
      if (!days.has(cursor.toISOString().slice(0, 10))) {
        return 0;
      }
    }

    while (days.has(cursor.toISOString().slice(0, 10))) {
      streak++;
      cursor.setDate(cursor.getDate() - 1);
    }
    return streak;
  }

  private persist(): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this._read()));
  }
}
