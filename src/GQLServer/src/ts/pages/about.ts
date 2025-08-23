import Swal from 'sweetalert2';
import _ from 'lodash';

class AboutPage {
  constructor() {
    this.initializeButtons();
    this.initializeTimeline();
    this.initializeTeamCards();
  }

  private initializeButtons(): void {
    // Add test button functionality
    const testButton = document.getElementById('about-test-button');
    if (testButton) {
      testButton.addEventListener('click', () => {
        Swal.fire({
          title: 'About Page',
          text: 'You are currently on the About page!',
          icon: 'info',
          confirmButtonText: 'Great!',
          confirmButtonColor: '#764ba2'
        });
      });
    }
  }

  private initializeTimeline(): void {
    const observerOptions: IntersectionObserverInit = {
      threshold: 0.1,
      rootMargin: '0px 0px -100px 0px'
    };
    
    const observer = new IntersectionObserver((entries) => {
      entries.forEach(entry => {
        if (entry.isIntersecting) {
          entry.target.classList.add('opacity-100', 'translate-y-0');
          entry.target.classList.remove('opacity-0', 'translate-y-4');
        }
      });
    }, observerOptions);
    
    // Add initial classes and observe timeline items
    const timelineItems = document.querySelectorAll('.timeline-item');
    timelineItems.forEach(item => {
      item.classList.add('opacity-0', 'translate-y-4', 'transition-all', 'duration-500');
      observer.observe(item);
    });
  }

  private initializeTeamCards(): void {
    const teamCards = document.querySelectorAll('.team-card');
    
    teamCards.forEach(card => {
      card.addEventListener('click', () => {
        const memberName = card.querySelector('.team-name')?.textContent || 'Team Member';
        const memberRole = card.querySelector('.team-role')?.textContent || 'Role';
        
        Swal.fire({
          title: memberName,
          text: memberRole,
          icon: 'info',
          confirmButtonText: 'Nice!',
          confirmButtonColor: '#667eea'
        });
      });
    });

    // Add hover effect
    teamCards.forEach(card => {
      card.addEventListener('mouseenter', () => {
        card.classList.add('transform', 'scale-105');
      });
      
      card.addEventListener('mouseleave', () => {
        card.classList.remove('transform', 'scale-105');
      });
    });
  }

  public animateStatistics(): void {
    const stats = document.querySelectorAll('[data-stat-value]');
    
    stats.forEach(stat => {
      const element = stat as HTMLElement;
      const value = element.getAttribute('data-stat-value');
      
      if (value && !isNaN(Number(value))) {
        this.countUp(element, 0, parseInt(value), 2000);
      }
    });
  }

  private countUp(element: HTMLElement, start: number, end: number, duration: number): void {
    const range = end - start;
    const increment = end > start ? 1 : -1;
    const stepTime = Math.abs(Math.floor(duration / range));
    let current = start;
    
    const timer = setInterval(() => {
      current += increment;
      element.textContent = current.toString();
      
      if (current === end) {
        clearInterval(timer);
      }
    }, stepTime);
  }

  public initializeTechStack(): void {
    const techItems = document.querySelectorAll('.tech-item');
    
    // Debounced search functionality
    const searchInput = document.getElementById('tech-search') as HTMLInputElement;
    if (searchInput) {
      const filterTech = _.debounce((searchTerm: string) => {
        techItems.forEach(item => {
          const techName = item.querySelector('.tech-name')?.textContent?.toLowerCase() || '';
          const techCategory = item.querySelector('.tech-category')?.textContent?.toLowerCase() || '';
          
          if (techName.includes(searchTerm.toLowerCase()) || 
              techCategory.includes(searchTerm.toLowerCase())) {
            (item as HTMLElement).style.display = 'block';
          } else {
            (item as HTMLElement).style.display = 'none';
          }
        });
      }, 300);

      searchInput.addEventListener('input', (e) => {
        filterTech((e.target as HTMLInputElement).value);
      });
    }
  }
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    const aboutPage = new AboutPage();
    aboutPage.animateStatistics();
    aboutPage.initializeTechStack();
  });
} else {
  const aboutPage = new AboutPage();
  aboutPage.animateStatistics();
  aboutPage.initializeTechStack();
}

export default AboutPage;