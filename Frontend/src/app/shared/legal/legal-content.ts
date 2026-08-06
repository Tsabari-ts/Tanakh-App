export type LegalDocId = 'terms' | 'privacy' | 'accessibility';

export interface LegalDocMeta {
  id: LegalDocId;
  title: string;
  lastUpdated: string;
  /** Icon path data for the dialog header badge (viewBox 0 0 24 24). */
  iconPaths: string[];
}

export interface LegalDoc extends LegalDocMeta {
  /** Clean HTML only - no <script>, no inline styles. Static/developer-authored content only. */
  html: string;
}

// Metadata only (small, eagerly bundled) - the full HTML bodies live in
// legal-content-html.ts and are loaded via dynamic import (see
// legal-dialog.service.ts) to keep them out of the main chunk. Components
// that only need e.g. a version stamp for consent logging (subscribe.component)
// can import this file without pulling in ~20kb of legal text.
export const LEGAL_DOCS_META: Record<LegalDocId, LegalDocMeta> = {
  terms: {
    id: 'terms',
    title: 'תנאי שימוש',
    lastUpdated: '2026-08-06',
    iconPaths: [
      'M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z',
      'M14 2v6h6M9 13h6M9 17h4',
    ],
  },
  privacy: {
    id: 'privacy',
    title: 'מדיניות פרטיות',
    lastUpdated: '2026-08-06',
    iconPaths: [
      'M12 2 4 5v6c0 5 3.4 9.4 8 11 4.6-1.6 8-6 8-11V5z',
    ],
  },
  accessibility: {
    id: 'accessibility',
    title: 'הצהרת נגישות',
    lastUpdated: '2026-08-02',
    iconPaths: [
      'M4 8h16M9 22l3-8 3 8M12 8v6',
    ],
  },
};
