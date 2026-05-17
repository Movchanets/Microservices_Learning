// OrderService unit tests.
// Verifies HTTP calls to the BFF gateway: GET /api/orders/{id},
// GET /api/orders/buyer/{buyerId}, and GET /api/payments/order/{orderId}.
// Uses HttpClientTestingModule to assert correct URLs and methods.

import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { OrderService } from './order.service';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [OrderService],
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getOrderById', () => {
    it('should GET /api/orders/{id}', async () => {
      const mockOrder = { id: 'order-1', status: 'Completed' };
      const promise = service.getOrderById('order-1');

      const req = httpMock.expectOne('/api/orders/order-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockOrder);

      const result = await promise;
      expect(result).toEqual(mockOrder);
    });
  });

  describe('getOrdersByBuyer', () => {
    it('should GET /api/orders/buyer/{buyerId}', async () => {
      const mockOrders = [{ id: 'order-1' }, { id: 'order-2' }];
      const promise = service.getOrdersByBuyer('buyer-1');

      const req = httpMock.expectOne('/api/orders/buyer/buyer-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockOrders);

      const result = await promise;
      expect(result).toEqual(mockOrders);
    });
  });

  describe('getPaymentStatus', () => {
    it('should GET /api/payments/order/{orderId}', async () => {
      const mockPayment = { id: 'pay-1', orderId: 'order-1', status: 'Completed' };
      const promise = service.getPaymentStatus('order-1');

      const req = httpMock.expectOne('/api/payments/order/order-1');
      expect(req.request.method).toBe('GET');
      req.flush(mockPayment);

      const result = await promise;
      expect(result).toEqual(mockPayment);
    });
  });

  describe('cancelOrder', () => {
    it('should POST /api/orders/{id}/cancel with reason', async () => {
      const promise = service.cancelOrder('order-1', 'changed mind');

      const req = httpMock.expectOne('/api/orders/order-1/cancel');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ reason: 'changed mind' });
      req.flush(null);

      await promise;
    });

    it('should POST without reason when not provided', async () => {
      const promise = service.cancelOrder('order-1');

      const req = httpMock.expectOne('/api/orders/order-1/cancel');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ reason: undefined });
      req.flush(null);

      await promise;
    });
  });

  describe('updateOrderStatus', () => {
    it('should PUT /api/orders/{id}/status with status and notes', async () => {
      const promise = service.updateOrderStatus('order-1', 'Shipped', 'Left warehouse');

      const req = httpMock.expectOne('/api/orders/order-1/status');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ status: 'Shipped', notes: 'Left warehouse' });
      req.flush(null);

      await promise;
    });

    it('should PUT without notes when not provided', async () => {
      const promise = service.updateOrderStatus('order-1', 'Processing');

      const req = httpMock.expectOne('/api/orders/order-1/status');
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual({ status: 'Processing', notes: undefined });
      req.flush(null);

      await promise;
    });
  });
});
