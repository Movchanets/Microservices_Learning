import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ToastContainerComponent } from './toast-container';
import { ToastService } from '../../../core/services/toast.service';

describe('ToastContainerComponent', () => {
  let component: ToastContainerComponent;
  let fixture: ComponentFixture<ToastContainerComponent>;
  let toastService: ToastService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ToastContainerComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ToastContainerComponent);
    component = fixture.componentInstance;
    toastService = TestBed.inject(ToastService);
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render nothing when no toasts', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('[class*="rounded-xl"]').length).toBe(0);
  });

  it('should render a toast when added', () => {
    toastService.show('Test toast', 'info');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Test toast');
  });

  it('should render error toast with correct styling', () => {
    toastService.error('Error occurred');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const toast = compiled.querySelector('[class*="red"]');
    expect(toast).toBeTruthy();
    expect(compiled.textContent).toContain('Error occurred');
  });

  it('should render success toast with correct styling', () => {
    toastService.success('It worked');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const toast = compiled.querySelector('[class*="green"]');
    expect(toast).toBeTruthy();
  });

  it('should dismiss toast on button click', () => {
    toastService.show('Dismissible');
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button');
    button.click();
    fixture.detectChanges();

    expect(toastService.toasts()).toHaveLength(0);
  });
});
