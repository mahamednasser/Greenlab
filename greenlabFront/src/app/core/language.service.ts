import { Injectable } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { firstValueFrom } from 'rxjs';

const STORAGE_KEY = 'greenlab_lang';

export function applyLangToDocument(lang: string): void {
  const isAr = lang === 'ar';
  document.documentElement.setAttribute('lang', lang);
  document.documentElement.setAttribute('dir', isAr ? 'rtl' : 'ltr');
  document.body.classList.toggle('rtl', isAr);
}

/** Runs before app bootstrap; deps: TranslateService */
export function i18nAppInitializerFactory(translate: TranslateService) {
  return () => {
    translate.addLangs(['en', 'ar']);
    translate.setDefaultLang('en');
    const saved = localStorage.getItem(STORAGE_KEY);
    const browser =
      typeof navigator !== 'undefined' && navigator.language?.toLowerCase().startsWith('ar')
        ? 'ar'
        : 'en';
    const lang = saved === 'ar' || saved === 'en' ? saved : browser;
    applyLangToDocument(lang);
    return firstValueFrom(translate.use(lang));
  };
}

@Injectable({ providedIn: 'root' })
export class LanguageService {
  constructor(private readonly translate: TranslateService) {}

  get current(): string {
    return this.translate.currentLang || 'en';
  }

  isArabic(): boolean {
    return this.current === 'ar';
  }

  setLanguage(lang: 'en' | 'ar'): void {
    this.translate.use(lang).subscribe(() => {
      localStorage.setItem(STORAGE_KEY, lang);
      applyLangToDocument(lang);
    });
  }

  toggle(): void {
    this.setLanguage(this.isArabic() ? 'en' : 'ar');
  }
}
