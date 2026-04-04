export interface ProductDto {
  productId: number;
  name: string;
  price: number;
  description: string;
  categoryName: string;
  imageUrl?: string;
  imageLocalPath?: string;
  count?: number;
  image?: File;
}