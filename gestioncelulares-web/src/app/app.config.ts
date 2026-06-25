import { ApplicationConfig, LOCALE_ID, importProvidersFrom, provideBrowserGlobalErrorListeners, provideZoneChangeDetection } from '@angular/core';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { registerLocaleData } from '@angular/common';
import localeEsDO from '@angular/common/locales/es-DO';
import {
  LucideAngularModule,
  Smartphone, User, Lock, LayoutDashboard, ShoppingCart, Package, Users,
  CreditCard, Wrench, Wallet, BarChart3, Settings, LogOut, TrendingUp,
  Boxes, Trophy, Medal, ArrowUpRight,
  Search, Plus, Pencil, Ban, ShieldCheck, X, Phone, Mail, MapPin,
  CircleAlert, Check, Loader, UserPlus,
  ScanLine, PackagePlus, Tag, Hash, CircleCheck, CircleX,
  PanelLeftClose, PanelLeftOpen,
  ArrowDownLeft, Calculator, Banknote, LockKeyhole, DoorOpen, Clock,
  ChevronDown, ChevronRight, Layers, Bookmark, Folder, CircleDot,
  Trash2, Percent, Receipt, Minus, TriangleAlert,
  CalendarClock, Coins, RefreshCw, CircleDollarSign,
  Image, ArrowRight, UserCog, ClipboardList, Camera,
  Download, FileText, Calendar, Printer, SlidersHorizontal,
  Truck, Building2, HandCoins, ShieldAlert, RefreshCcw,
  PackageX, ClipboardCheck, Volume2, VolumeX,
  WandSparkles, Scale, Eye
} from 'lucide-angular';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth-interceptor';

registerLocaleData(localeEsDO);

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideAnimations(),
    provideHttpClient(withInterceptors([authInterceptor])),
    { provide: LOCALE_ID, useValue: 'es-DO' },
    importProvidersFrom(
      LucideAngularModule.pick({
        Smartphone, User, Lock, LayoutDashboard, ShoppingCart, Package, Users,
        CreditCard, Wrench, Wallet, BarChart3, Settings, LogOut, TrendingUp,
        Boxes, Trophy, Medal, ArrowUpRight,
        Search, Plus, Pencil, Ban, ShieldCheck, X, Phone, Mail, MapPin,
        CircleAlert, Check, Loader, UserPlus,
        ScanLine, PackagePlus, Tag, Hash, CircleCheck, CircleX,
        PanelLeftClose, PanelLeftOpen,
        ArrowDownLeft, Calculator, Banknote, LockKeyhole, DoorOpen, Clock,
        ChevronDown, ChevronRight, Layers, Bookmark, Folder, CircleDot,
        Trash2, Percent, Receipt, Minus, TriangleAlert,
        CalendarClock, Coins, RefreshCw, CircleDollarSign,
        Image, ArrowRight, UserCog, ClipboardList, Camera,
        Download, FileText, Calendar, Printer, SlidersHorizontal,
        Truck, Building2, HandCoins, ShieldAlert, RefreshCcw,
        PackageX, ClipboardCheck, Volume2, VolumeX,
        WandSparkles, Scale, Eye
      })
    )
  ]
};
