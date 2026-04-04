import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { AuthState } from '../../../core/services/auth-state.service';
import { CartService } from '../../../core/services/cart.service';
import { SD } from '../../../constants/app.constants';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-navbar',
  imports: [RouterLinkActive, RouterLink, CommonModule],
  templateUrl: './navbar.html',
  styleUrl: './navbar.scss',
})
export class Navbar {
  SD = SD;

  constructor(
    public authState: AuthState,
    private authService: AuthService,
    public cartService: CartService,
  ) { }

  ngOnInit(): void {
    console.log(222222222222222222222)
    const user = this.authState.currentUser;
    if (user) {
      this.cartService.getCartByUserId(user.id).subscribe();
    }
  }

  logout(): void {
    this.authService.logout();
  }
}
