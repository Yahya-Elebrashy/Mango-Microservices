import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CouponDelete } from './coupon-delete';

describe('CouponDelete', () => {
  let component: CouponDelete;
  let fixture: ComponentFixture<CouponDelete>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CouponDelete],
    }).compileComponents();

    fixture = TestBed.createComponent(CouponDelete);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
