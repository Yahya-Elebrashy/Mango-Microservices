export interface LoginRequestDto {
  userName: string;
  password: string;
}

export interface LoginResponseDto {
  user: UserDto;
  token: string;
}

export interface RegisterRequestDto {
  email: string;
  name: string;
  phoneNumber: string;
  password: string;
  role?: string;
}

export interface UserDto {
  id: string;
  email: string;
  name: string;
  phoneNumber: string;
}