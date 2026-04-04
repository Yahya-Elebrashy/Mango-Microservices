import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CouponList } from './coupon-list';

describe('CouponList', () => {
  let component: CouponList;
  let fixture: ComponentFixture<CouponList>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CouponList],
    }).compileComponents();

    fixture = TestBed.createComponent(CouponList);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
