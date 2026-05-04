const natsNamePattern = /^[A-Za-z0-9._-]+$/;
const natsSubjectPattern = /^[A-Za-z0-9._*>-]+$/;

export function validateNatsName(value: string, fieldName = 'Name', maxLength = 256): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return `${fieldName} is required`;
  if (value.length > maxLength) return `${fieldName} must be ${maxLength} characters or fewer`;
  if (value !== trimmed) return `${fieldName} must not start or end with whitespace`;
  if (!natsNamePattern.test(value)) return `${fieldName} can only contain letters, numbers, dots, hyphens, and underscores`;
  return null;
}

export function validateNatsSubject(value: string, fieldName = 'Subject'): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return `${fieldName} is required`;
  if (value !== trimmed) return `${fieldName} must not contain whitespace`;
  if (/\s/.test(value)) return `${fieldName} must not contain whitespace`;
  if (!natsSubjectPattern.test(value)) return `${fieldName} can only contain letters, numbers, dots, hyphens, underscores, * and >`;
  return null;
}

export function validateOptionalNatsSubject(value: string, fieldName = 'Subject'): string | null {
  return value.trim().length === 0 ? null : validateNatsSubject(value, fieldName);
}

export function validateInteger(value: number | string, fieldName: string, min: number, max?: number): string | null {
  if (value === '') return `${fieldName} is required`;
  const numberValue = Number(value);
  if (!Number.isInteger(numberValue)) return `${fieldName} must be a whole number`;
  if (numberValue < min) return `${fieldName} must be at least ${min}`;
  if (max !== undefined && numberValue > max) return `${fieldName} must be at most ${max}`;
  return null;
}

export function validateUnlimitedInteger(value: number | string, fieldName: string, min: number, max?: number): string | null {
  if (value === '') return `${fieldName} is required`;
  const numberValue = Number(value);
  if (!Number.isInteger(numberValue)) return `${fieldName} must be a whole number`;
  if (numberValue !== -1 && numberValue < min) return `${fieldName} must be -1 or at least ${min}`;
  if (max !== undefined && numberValue > max) return `${fieldName} must be at most ${max}`;
  return null;
}

export function validateNonNegativeInteger(value: number | string, fieldName: string): string | null {
  return validateInteger(value, fieldName, 0);
}

export function validateObjectName(value: string, fieldName = 'Object name'): string | null {
  const trimmed = value.trim();
  if (trimmed.length === 0) return `${fieldName} is required`;
  if (trimmed.length > 512) return `${fieldName} must be 512 characters or fewer`;
  if (trimmed.includes('/') || trimmed.startsWith('~') || trimmed.includes('..') || Array.from(trimmed).some((char) => char.charCodeAt(0) < 32)) {
    return `${fieldName} contains invalid path characters`;
  }
  return null;
}

export function validatePassword(value: string): string | null {
  if (value.length === 0) return 'Password is required';
  if (value.length < 12) return 'Password must be at least 12 characters long';
  if (value.length > 256) return 'Password must be 256 characters or fewer';
  if (!/[A-Z]/.test(value)) return 'Password must contain at least one uppercase letter';
  if (!/[a-z]/.test(value)) return 'Password must contain at least one lowercase letter';
  if (!/[0-9]/.test(value)) return 'Password must contain at least one digit';
  if (!/[^A-Za-z0-9]/.test(value)) return 'Password must contain at least one non-alphanumeric character';
  return null;
}
