import _ from 'lodash';
import axios from 'axios';
import dayjs from 'dayjs';
import Swal from 'sweetalert2';

// Initialize global namespace
declare global {
  interface Window {
    GQLServer: GQLServerGlobal;
  }
}

class GQLServerApp {
  private static instance: GQLServerApp;

  private constructor() {
    this.initializeGlobalNamespace();
    this.initializeEventListeners();
    this.initializeAPI();
  }

  public static getInstance(): GQLServerApp {
    if (!GQLServerApp.instance) {
      GQLServerApp.instance = new GQLServerApp();
    }
    return GQLServerApp.instance;
  }

  private initializeGlobalNamespace(): void {
    window.GQLServer = {
      api: {
        graphql: this.graphqlRequest.bind(this),
        rest: this.restRequest.bind(this)
      },
      utils: {
        debounce: _.debounce,
        throttle: _.throttle,
        formatDate: this.formatDate.bind(this),
        formatCurrency: this.formatCurrency.bind(this)
      },
      showNotification: this.showNotification.bind(this),
      currentPage: this.detectCurrentPage()
    };
  }

  private detectCurrentPage(): string {
    const path = window.location.pathname;
    if (path === '/') return 'home';
    if (path === '/about') return 'about';
    if (path.startsWith('/activate-account')) return 'activate-account';
    return 'unknown';
  }

  private initializeEventListeners(): void {
    // Mobile menu toggle
    const mobileMenuButton = document.querySelector('.mobile-menu-button');
    if (mobileMenuButton) {
      mobileMenuButton.addEventListener('click', this.toggleMobileMenu.bind(this));
    }

    // Initialize tooltips
    this.initializeTooltips();

    // Initialize modals
    this.initializeModals();
  }

  private toggleMobileMenu(): void {
    const menu = document.getElementById('mobile-menu');
    if (!menu) return;

    const isOpen = !menu.classList.contains('hidden');
    const button = document.querySelector('.mobile-menu-button');
    
    if (isOpen) {
      menu.classList.add('hidden');
      button?.querySelector('svg:first-child')?.classList.remove('hidden');
      button?.querySelector('svg:last-child')?.classList.add('hidden');
    } else {
      menu.classList.remove('hidden');
      button?.querySelector('svg:first-child')?.classList.add('hidden');
      button?.querySelector('svg:last-child')?.classList.remove('hidden');
    }
  }

  private initializeTooltips(): void {
    const tooltips = document.querySelectorAll('[data-tooltip]');
    tooltips.forEach(element => {
      element.addEventListener('mouseenter', this.showTooltip.bind(this));
      element.addEventListener('mouseleave', this.hideTooltip.bind(this));
    });
  }

  private showTooltip(event: Event): void {
    const target = event.target as HTMLElement;
    const text = target.getAttribute('data-tooltip');
    if (!text) return;

    const tooltip = document.createElement('div');
    tooltip.className = 'absolute bg-gray-900 text-white text-sm rounded px-2 py-1 z-50';
    tooltip.textContent = text;
    tooltip.id = 'tooltip';
    
    document.body.appendChild(tooltip);
    this.positionTooltip(tooltip, target);
  }

  private hideTooltip(): void {
    const tooltip = document.getElementById('tooltip');
    tooltip?.remove();
  }

  private positionTooltip(tooltip: HTMLElement, target: HTMLElement): void {
    const rect = target.getBoundingClientRect();
    tooltip.style.top = `${rect.top - tooltip.offsetHeight - 5}px`;
    tooltip.style.left = `${rect.left + (rect.width - tooltip.offsetWidth) / 2}px`;
  }

  private initializeModals(): void {
    // Modal triggers
    const modalTriggers = document.querySelectorAll('[data-modal]');
    modalTriggers.forEach(trigger => {
      trigger.addEventListener('click', this.openModal.bind(this));
    });

    // Close modal on overlay click
    document.addEventListener('click', (e) => {
      const target = e.target as HTMLElement;
      if (target.classList.contains('modal-overlay') || target.classList.contains('modal-close')) {
        this.closeModal();
      }
    });

    // Close modal on Escape key
    document.addEventListener('keydown', (e) => {
      if (e.key === 'Escape') {
        this.closeModal();
      }
    });
  }

  private openModal(event: Event): void {
    event.preventDefault();
    const trigger = event.currentTarget as HTMLElement;
    const modalId = trigger.getAttribute('data-modal');
    if (!modalId) return;

    const modal = document.querySelector(modalId);
    if (modal) {
      modal.classList.add('active');
      document.body.style.overflow = 'hidden';
    }
  }

  private closeModal(): void {
    const activeModal = document.querySelector('.modal.active');
    if (activeModal) {
      activeModal.classList.remove('active');
      document.body.style.overflow = '';
    }
  }

  private initializeAPI(): void {
    // Set up axios defaults
    axios.defaults.headers.common['X-Requested-With'] = 'XMLHttpRequest';
    
    const token = localStorage.getItem('authToken');
    if (token) {
      axios.defaults.headers.common['Authorization'] = `Bearer ${token}`;
    }

    // Add response interceptor for auth
    axios.interceptors.response.use(
      response => response,
      error => {
        if (error.response?.status === 401) {
          // Handle unauthorized
          localStorage.removeItem('authToken');
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }
    );
  }

  private async graphqlRequest(query: string, variables: Record<string, any> = {}): Promise<any> {
    try {
      const response = await axios.post('/graphql', { query, variables });
      
      if (response.data.errors) {
        throw new Error(response.data.errors[0].message);
      }
      
      return response.data.data;
    } catch (error) {
      console.error('GraphQL request failed:', error);
      throw error;
    }
  }

  private async restRequest(endpoint: string, options: any = {}): Promise<any> {
    try {
      const response = await axios({
        url: endpoint,
        method: options.method || 'GET',
        data: options.body,
        headers: options.headers,
        ...options
      });
      return response.data;
    } catch (error) {
      console.error('REST request failed:', error);
      throw error;
    }
  }

  private formatDate(date: Date | string, format: string = 'YYYY-MM-DD'): string {
    return dayjs(date).format(format);
  }

  private formatCurrency(amount: number, currency: string = 'USD'): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: currency
    }).format(amount);
  }

  public showNotification(type: 'success' | 'error' | 'warning' | 'info', message: string, duration: number = 5000): void {
    const config: any = {
      toast: true,
      position: 'top-end',
      showConfirmButton: false,
      timer: duration,
      timerProgressBar: true,
      icon: type,
      title: message
    };

    Swal.fire(config);
  }
}

// Initialize app when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    GQLServerApp.getInstance();
  });
} else {
  GQLServerApp.getInstance();
}

export default GQLServerApp;