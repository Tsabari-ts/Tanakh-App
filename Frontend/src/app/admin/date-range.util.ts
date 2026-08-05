import { DateRange, DateRangePreset } from './admin.models';

export function resolveDateRange(preset: DateRangePreset): DateRange {
  const to = new Date();
  const from = new Date(to);

  switch (preset) {
    case 'today':
      from.setHours(0, 0, 0, 0);
      break;
    case '7d':
      from.setDate(from.getDate() - 7);
      break;
    case '30d':
      from.setDate(from.getDate() - 30);
      break;
  }

  return { from: from.toISOString(), to: to.toISOString() };
}
