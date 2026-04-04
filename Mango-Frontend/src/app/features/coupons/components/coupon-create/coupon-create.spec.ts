import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CouponCreate } from './coupon-create';

describe('CouponCreate', () => {
  let component: CouponCreate;
  let fixture: ComponentFixture<CouponCreate>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CouponCreate],
    }).compileComponents();

    fixture = TestBed.createComponent(CouponCreate);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
