import { DatePipe } from '@angular/common';
import { Component, ChangeDetectionStrategy, DestroyRef, ViewChild, AfterViewInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { AdminSmsService } from '../admin-sms.service';
import { AdminExportService } from '../admin-export.service';
import { SmsBalanceInfo, SmsLogItem, SmsStats } from '../admin.models';

@Component({
  selector: 'app-admin-sms',
  templateUrl: './admin-sms.component.html',
  styleUrl: './admin-sms.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, MatTableModule, MatPaginatorModule]
})
export class AdminSmsComponent implements AfterViewInit {
  private readonly smsService = inject(AdminSmsService);
  private readonly exportService = inject(AdminExportService);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  readonly displayedColumns = ['createdAt', 'maskedPhone', 'type', 'success', 'providerStatusCode'];
  readonly balance = signal<SmsBalanceInfo | null>(null);
  readonly stats = signal<SmsStats | null>(null);
  readonly items = signal<SmsLogItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);
  readonly testSending = signal(false);
  readonly testSentMessage = signal('');

  status = '';

  private page = 1;
  private limit = 25;

  ngAfterViewInit(): void {
    this.loadBalance();
    this.loadStats();
    this.loadLog();
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.limit = event.pageSize;
    this.loadLog();
  }

  applyFilters(): void {
    this.page = 1;
    if (this.paginator) {
      this.paginator.pageIndex = 0;
    }
    this.loadLog();
  }

  sendTest(): void {
    this.testSending.set(true);
    this.testSentMessage.set('');

    this.smsService.sendTest()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: () => {
          this.testSending.set(false);
          this.testSentMessage.set('הודעת הבדיקה נשלחה.');
          this.loadLog();
        },
        error: () => {
          this.testSending.set(false);
          this.testSentMessage.set('שליחת הודעת הבדיקה נכשלה.');
        }
      });
  }

  exportCsv(): void {
    const params = this.smsService.exportParams({ status: this.status });
    this.exportService.download('sms-log', params)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  private loadBalance(): void {
    this.smsService.getBalance()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(balance => this.balance.set(balance));
  }

  private loadStats(): void {
    this.smsService.getStats()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(stats => this.stats.set(stats));
  }

  private loadLog(): void {
    this.loading.set(true);

    this.smsService.getLog({ status: this.status, page: this.page, limit: this.limit })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: result => {
          this.items.set(result.items);
          this.totalCount.set(result.totalCount);
          this.loading.set(false);
        },
        error: () => this.loading.set(false)
      });
  }
}
