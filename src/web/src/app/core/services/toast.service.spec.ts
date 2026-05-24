import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let service: ToastService;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({});
    service = TestBed.inject(ToastService);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('should start with empty toasts', () => {
    expect(service.toasts()).toEqual([]);
  });

  it('show should add a toast', () => {
    service.show('Test message', 'info');
    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].message).toBe('Test message');
    expect(service.toasts()[0].type).toBe('info');
  });

  it('error should add an error toast', () => {
    service.error('Error message');
    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].type).toBe('error');
  });

  it('success should add a success toast', () => {
    service.success('Success message');
    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].type).toBe('success');
  });

  it('dismiss should remove a toast by id', () => {
    service.show('Toast 1');
    service.show('Toast 2');
    expect(service.toasts()).toHaveLength(2);

    const id = service.toasts()[0].id;
    service.dismiss(id);
    expect(service.toasts()).toHaveLength(1);
    expect(service.toasts()[0].message).toBe('Toast 2');
  });

  it('should auto-dismiss after duration', () => {
    service.show('Auto dismiss', 'info', 1000);
    expect(service.toasts()).toHaveLength(1);

    vi.advanceTimersByTime(1000);
    expect(service.toasts()).toHaveLength(0);
  });

  it('should increment ids', () => {
    service.show('First');
    service.show('Second');
    expect(service.toasts()[0].id).toBeLessThan(service.toasts()[1].id);
  });
});
