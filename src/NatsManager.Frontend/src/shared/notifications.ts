import { notifications } from '@mantine/notifications';

export function showSuccess(message: string, title = 'Success') {
  notifications.show({
    title,
    message,
    color: 'green',
    autoClose: 4000,
  });
}

export function showError(message: string, title = 'Error') {
  notifications.show({
    title,
    message,
    color: 'red',
    autoClose: 6000,
  });
}

export function showWarning(message: string, title = 'Warning') {
  notifications.show({
    title,
    message,
    color: 'yellow',
    autoClose: 5000,
  });
}
