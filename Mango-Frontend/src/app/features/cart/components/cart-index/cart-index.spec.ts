import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CartIndex } from './cart-index';

describe('CartIndex', () => {
  let component: CartIndex;
  let fixture: ComponentFixture<CartIndex>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CartIndex],
    }).compileComponents();

    fixture = TestBed.createComponent(CartIndex);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
