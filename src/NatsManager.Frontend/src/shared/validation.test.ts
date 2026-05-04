import {
  validateInteger,
  validateNatsName,
  validateNatsSubject,
  validateObjectName,
  validatePassword,
  validateUnlimitedInteger,
} from './validation';

describe('validation utilities', () => {
  it('validates NATS resource names', () => {
    expect(validateNatsName('orders.prod_1', 'Name')).toBeNull();
    expect(validateNatsName('orders prod', 'Name')).toBe('Name can only contain letters, numbers, dots, hyphens, and underscores');
  });

  it('validates NATS subjects', () => {
    expect(validateNatsSubject('orders.*')).toBeNull();
    expect(validateNatsSubject('orders with spaces')).toBe('Subject must not contain whitespace');
  });

  it('validates bounded and unlimited integers', () => {
    expect(validateInteger(3, 'Replicas', 1, 5)).toBeNull();
    expect(validateInteger(6, 'Replicas', 1, 5)).toBe('Replicas must be at most 5');
    expect(validateUnlimitedInteger(-1, 'Max Bytes', 1)).toBeNull();
    expect(validateUnlimitedInteger(0, 'Max Bytes', 1)).toBe('Max Bytes must be -1 or at least 1');
  });

  it('validates object names', () => {
    expect(validateObjectName('my-object.txt')).toBeNull();
    expect(validateObjectName('path/to/object.txt')).toBe('Object name contains invalid path characters');
    expect(validateObjectName('../secret')).toBe('Object name contains invalid path characters');
  });

  it('validates password policy', () => {
    expect(validatePassword('ValidPass123!')).toBeNull();
    expect(validatePassword('short')).toBe('Password must be at least 12 characters long');
  });
});
