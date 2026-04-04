import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ProductDelete } from './product-delete';

describe('ProductDelete', () => {
  let component: ProductDelete;
  let fixture: ComponentFixture<ProductDelete>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ProductDelete],
    }).compileComponents();

    fixture = TestBed.createComponent(ProductDelete);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
