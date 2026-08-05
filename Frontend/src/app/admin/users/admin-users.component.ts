import { DatePipe } from '@angular/common';
import { Component, ChangeDetectionStrategy, DestroyRef, ViewChild, AfterViewInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { AdminUsersService } from '../admin-users.service';
import { AdminExportService } from '../admin-export.service';
import { UserListItem } from '../admin.models';
import { ConfirmDialogComponent } from '../shared/confirm-dialog.component';

@Component({
  selector: 'app-admin-users',
  templateUrl: './admin-users.component.html',
  styleUrl: './admin-users.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, MatTableModule, MatPaginatorModule]
})
export class AdminUsersComponent implements AfterViewInit {
  private readonly usersService = inject(AdminUsersService);
  private readonly exportService = inject(AdminExportService);
  private readonly dialog = inject(MatDialog);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild(MatPaginator) paginator!: MatPaginator;

  readonly displayedColumns = ['phoneNumber', 'displayName', 'status', 'createdAt', 'actions'];
  readonly items = signal<UserListItem[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(false);

  search = '';
  status = '';

  private page = 1;
  private limit = 25;

  ngAfterViewInit(): void {
    this.load();
  }

  onPage(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.limit = event.pageSize;
    this.load();
  }

  applyFilters(): void {
    this.page = 1;
    if (this.paginator) {
      this.paginator.pageIndex = 0;
    }
    this.load();
  }

  block(user: UserListItem): void {
    this.usersService.block(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  unblock(user: UserListItem): void {
    this.usersService.unblock(user.id)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.load());
  }

  delete(user: UserListItem): void {
    this.dialog.open(ConfirmDialogComponent, {
      width: '400px',
      data: {
        title: 'מחיקת משתמש',
        message: 'הפעולה תמחק את פרטי הקשר של המשתמש (טלפון ושם). הפעולה אינה הפיכה.',
        confirmLabel: 'מחיקה'
      }
    }).afterClosed()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(confirmed => {
        if (confirmed) {
          this.usersService.delete(user.id)
            .pipe(takeUntilDestroyed(this.destroyRef))
            .subscribe(() => this.load());
        }
      });
  }

  exportCsv(): void {
    const params = this.usersService.exportParams({ search: this.search, status: this.status });
    this.exportService.download('users', params)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe();
  }

  private load(): void {
    this.loading.set(true);

    this.usersService.getUsers({
      search: this.search,
      status: this.status,
      page: this.page,
      limit: this.limit
    })
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
