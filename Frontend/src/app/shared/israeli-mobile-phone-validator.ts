// Mirrors Backend/Tanakh.Domain/Validation/IsraeliMobilePhoneValidator.cs
// exactly - same regex, same normalization steps - so client and server
// validation can never silently drift apart.
const LOCAL_MOBILE_PATTERN = /^05[0-9]\d{7}$/;
const LANDLINE_PATTERN = /^0[23489]\d{7}$/;

export type PhoneValidationResult = 'valid' | 'empty' | 'landline' | 'invalid';

export interface PhoneValidation {
  result: PhoneValidationResult;
  /** E.164 form ("+972501234567") when result is 'valid', empty otherwise. */
  e164: string;
}

export function validateIsraeliMobilePhone(input: string | null | undefined): PhoneValidation {
  if (!input || !input.trim()) {
    return { result: 'empty', e164: '' };
  }

  let digits = input.replace(/\D/g, '');

  if (digits.startsWith('00972')) {
    digits = '0' + digits.slice(5);
  } else if (digits.startsWith('972') && digits.length === 12) {
    digits = '0' + digits.slice(3);
  }

  if (LANDLINE_PATTERN.test(digits)) {
    return { result: 'landline', e164: '' };
  }

  if (!LOCAL_MOBILE_PATTERN.test(digits)) {
    return { result: 'invalid', e164: '' };
  }

  return { result: 'valid', e164: '+972' + digits.slice(1) };
}
