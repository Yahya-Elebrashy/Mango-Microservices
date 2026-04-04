import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { ApiService } from "./api.service";
import { CouponDto, ResponseDto } from "../../models";
import { Observable } from "rxjs";

@Injectable({ providedIn: 'root' })
export class CouponService {

  private base = environment.couponApiBase;

  constructor(private api: ApiService) {}

   getAll(): Observable<ResponseDto<CouponDto[]>> {
    return this.api.get<CouponDto[]>(`${this.base}/api/coupon`);
  }

  getById(id: number): Observable<ResponseDto<CouponDto>> {
    return this.api.get<CouponDto>(`${this.base}/api/coupon/${id}`);
  }

  getByCode(code: string): Observable<ResponseDto<CouponDto>> {
    return this.api.get<CouponDto>(`${this.base}/api/coupon/GetByCode/${code}`);
  }

  create(coupon: CouponDto): Observable<ResponseDto<CouponDto>> {
    return this.api.post<CouponDto>(`${this.base}/api/coupon`, coupon);
  }

  update(coupon: CouponDto): Observable<ResponseDto<CouponDto>> {
    return this.api.put<CouponDto>(`${this.base}/api/coupon`, coupon);
  }

  delete(id: number): Observable<ResponseDto<unknown>> {
    return this.api.delete(`${this.base}/api/coupon/${id}`);
  }
}