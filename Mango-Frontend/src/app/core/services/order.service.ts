import { Injectable } from "@angular/core";
import { environment } from "../../../environments/environment";
import { CartDto, OrderHeaderDto, ResponseDto, StripeRequestDto } from "../../models";
import { Observable } from "rxjs";
import { OrderStatus } from "../../constants/app.constants";
import { ApiService } from "./api.service";

@Injectable({ providedIn: 'root' })
export class OrderService {

    private base = environment.orderApiBase;

    constructor(private api: ApiService) { }

    createOrder(cart: CartDto): Observable<ResponseDto<OrderHeaderDto>> {
        return this.api.post<OrderHeaderDto>(`${this.base}/api/order/CreateOrder`, cart);
    }

    getAllOrders(userId?: string): Observable<ResponseDto<OrderHeaderDto[]>> {
        const url = userId
            ? `${this.base}/api/order/GetOrders?userId=${userId}`
            : `${this.base}/api/order/GetOrders`;
        return this.api.get<OrderHeaderDto[]>(url);
    }

    getOrder(orderId: number): Observable<ResponseDto<OrderHeaderDto>> {
        return this.api.get<OrderHeaderDto>(`${this.base}/api/order/GetOrder/${orderId}`);
    }

    updateOrderStatus(orderId: number, status: OrderStatus): Observable<ResponseDto<unknown>> {
        return this.api.post(`${this.base}/api/order/UpdateOrderStatus/${orderId}`, status);
    }

    createStripeSession(request: StripeRequestDto): Observable<ResponseDto<StripeRequestDto>> {
        return this.api.post<StripeRequestDto>(
            `${this.base}/api/order/CreateStripeSession`,
            request
        );
    }

    validateStripeSession(orderId: number): Observable<ResponseDto<OrderHeaderDto>> {
        return this.api.post<OrderHeaderDto>(
            `${this.base}/api/order/ValidateStripeSession`,orderId 
        );
    }
}