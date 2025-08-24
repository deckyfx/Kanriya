import Swal from 'sweetalert2';

class HomePage {
  constructor() {
    this.initializeButtons();
    this.initializeFeatures();
  }

  private initializeButtons(): void {
    // Add test button functionality
    const testButton = document.getElementById('home-test-button');
    if (testButton) {
      testButton.addEventListener('click', () => {
        Swal.fire({
          title: 'Home Page',
          text: 'You are currently on the Home page!',
          icon: 'info',
          confirmButtonText: 'Cool!',
          confirmButtonColor: '#667eea'
        });
      });
    }

    // GraphQL playground button
    const graphqlButton = document.querySelector('[data-action="open-graphql"]');
    if (graphqlButton) {
      graphqlButton.addEventListener('click', (e) => {
        e.preventDefault();
        window.open('/graphql', '_blank');
      });
    }

    // Swagger button
    const swaggerButton = document.querySelector('[data-action="open-swagger"]');
    if (swaggerButton) {
      swaggerButton.addEventListener('click', (e) => {
        e.preventDefault();
        window.open('/swagger', '_blank');
      });
    }
  }

  private initializeFeatures(): void {
    // Animate feature cards on hover
    const featureCards = document.querySelectorAll('.feature-card');
    featureCards.forEach(card => {
      card.addEventListener('mouseenter', () => {
        card.classList.add('scale-105');
      });
      
      card.addEventListener('mouseleave', () => {
        card.classList.remove('scale-105');
      });
    });

    // Code example copy functionality
    const codeBlock = document.querySelector('.code-example pre');
    if (codeBlock) {
      codeBlock.addEventListener('click', async () => {
        const code = codeBlock.textContent || '';
        
        try {
          await navigator.clipboard.writeText(code);
          
          Swal.fire({
            toast: true,
            position: 'top-end',
            icon: 'success',
            title: 'Code copied to clipboard!',
            showConfirmButton: false,
            timer: 2000,
            timerProgressBar: true
          });
        } catch (err) {
          console.error('Failed to copy code:', err);
        }
      });

      // Add copy hint
      codeBlock.setAttribute('title', 'Click to copy');
      (codeBlock as HTMLElement).style.cursor = 'pointer';
    }
  }

  public async loadStats(): Promise<void> {
    // This could fetch real stats from the API
    const statsElements = document.querySelectorAll('[data-stat]');
    
    statsElements.forEach(element => {
      // Animate the numbers
      this.animateNumber(element as HTMLElement, 0, parseInt(element.textContent || '0'), 1000);
    });
  }

  private animateNumber(element: HTMLElement, start: number, end: number, duration: number): void {
    const range = end - start;
    const startTime = performance.now();
    
    const update = (currentTime: number) => {
      const elapsed = currentTime - startTime;
      const progress = Math.min(elapsed / duration, 1);
      
      const current = Math.floor(start + range * progress);
      element.textContent = current.toString();
      
      if (progress < 1) {
        requestAnimationFrame(update);
      }
    };
    
    requestAnimationFrame(update);
  }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    new HomePage();
  });
} else {
  new HomePage();
}

export default HomePage;