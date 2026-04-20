import { Injectable } from "@angular/core";
import { ApiService } from "./api.service";
import { environment } from "../../../environments/environment";
import { ProductDto, ResponseDto } from "../../models";
import { Observable } from "rxjs";

@Injectable({ providedIn: 'root' })
export class ProductService {

  private base = environment.productApiBase;

  constructor(private api: ApiService) {}

  getAll(): Observable<ResponseDto<ProductDto[]>> {
    return this.api.get<ProductDto[]>(`${this.base}/api/product`);
  }

  getById(id: number): Observable<ResponseDto<ProductDto>> {
    return this.api.get<ProductDto>(`${this.base}/api/product/${id}`);
  }

  create(product: ProductDto): Observable<ResponseDto<ProductDto>> {
    return this.api.post<ProductDto>(
      `${this.base}/api/product`,
      product,
      product.image ? 'multipart' : 'json'   // multipart only when image attached
    );
  }

  update(product: ProductDto): Observable<ResponseDto<ProductDto>> {
    return this.api.put<ProductDto>(
      `${this.base}/api/product`,
      product,
      product.image ? 'multipart' : 'json'
    );
  }

  delete(id: number): Observable<ResponseDto<unknown>> {
    return this.api.delete(`${this.base}/api/product/${id}`);
  }
}