export interface ResponseDto<T = unknown> {
  result: T | null;
  isSuccess: boolean;
  message: string;
}