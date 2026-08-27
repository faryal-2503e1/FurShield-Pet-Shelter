document.addEventListener('DOMContentLoaded', () => {
  const ARTICLES = {
    'dog-health': {
      title: '10 Simple Ways to Keep Your Dog Healthy',
      meta: 'Wellness · Aug 12, 2026',
      image: 'https://images.unsplash.com/photo-1558788353-f76d92427f16?auto=format&fit=crop&w=900&q=85',
      body: `<p>Healthy pets thrive on consistent routines, nutritious food, movement and preventive care.</p>
             <h2>1. Keep a regular routine</h2><p>Pets feel more secure when meals, walks and rest happen around predictable times.</p>
             <h2>2. Prioritize preventive care</h2><p>Routine checkups, dental care and grooming can help you spot issues early.</p>
             <h2>3. Make exercise fun</h2><p>Choose age-appropriate movement and enrichment that your dog enjoys.</p>
             <a class="btn btn-primary" href="booking.html">Book a Wellness Visit</a>`
    },
    'grooming-frequency': {
      title: 'How Often Should You Groom Your Pet?',
      meta: 'Grooming · Aug 08, 2026',
      image: 'https://images.unsplash.com/photo-1585110396000-c9ffd4e4b308?auto=format&fit=crop&w=900&q=85',
      body: `<p>Grooming needs vary by breed, coat type and lifestyle, but a few basics apply to every pet.</p>
             <h2>1. Brushing</h2><p>Most pets benefit from brushing a few times a week to reduce shedding and matting.</p>
             <h2>2. Bathing</h2><p>Every 4-6 weeks is a good baseline, adjusted for coat type and activity level.</p>
             <h2>3. Nails and ears</h2><p>Regular nail trims and ear checks help prevent discomfort and infection.</p>
             <a class="btn btn-primary" href="booking.html">Book a Grooming Session</a>`
    },
    'first-vet-visit': {
      title: 'Preparing Your Pet for Their First Vet Visit',
      meta: 'Vet Care · Aug 02, 2026',
      image: 'https://images.unsplash.com/photo-1452570053594-1b985d6ea890?auto=format&fit=crop&w=900&q=85',
      body: `<p>A calm, well-prepared first visit sets the tone for a lifetime of stress-free vet trips.</p>
             <h2>1. Get them comfortable with handling</h2><p>Practice gentle paw, ear and mouth touches at home beforehand.</p>
             <h2>2. Bring the essentials</h2><p>Carry vaccination records, a favorite toy and any recent health notes.</p>
             <h2>3. Stay calm yourself</h2><p>Pets pick up on our energy, so a relaxed owner helps keep them relaxed too.</p>
             <a class="btn btn-primary" href="booking.html">Book a Vet Visit</a>`
    },
    'pet-routine': {
      title: 'How to Build a Better Pet Routine',
      meta: 'Wellness · Jul 28, 2026',
      image: 'https://images.unsplash.com/photo-1425082661705-1834bfd09dca?auto=format&fit=crop&w=900&q=85',
      body: `<p>A predictable daily routine supports your pet's physical health and emotional wellbeing.</p>
             <h2>1. Consistent mealtimes</h2><p>Feeding at the same times each day supports digestion and reduces anxiety.</p>
             <h2>2. Daily movement</h2><p>Even short, regular walks or play sessions make a big difference.</p>
             <h2>3. Wind-down time</h2><p>A calm evening routine helps pets settle in for a good night's rest.</p>
             <a class="btn btn-primary" href="booking.html">Book a Wellness Check</a>`
    },
    'healthy-treats': {
      title: 'Healthy Treat Ideas for Dogs',
      meta: 'Nutrition · Jul 20, 2026',
      image: 'https://images.unsplash.com/photo-1589924691995-400dc9ecc119?auto=format&fit=crop&w=900&q=85',
      body: `<p>Treats are a great training tool and bonding moment when chosen with nutrition in mind.</p>
             <h2>1. Lean proteins</h2><p>Small pieces of cooked chicken or turkey make a simple, healthy reward.</p>
             <h2>2. Crunchy vegetables</h2><p>Carrot sticks or cucumber slices offer low-calorie, satisfying snacks.</p>
             <h2>3. Portion control</h2><p>Treats should make up no more than 10% of your dog's daily calories.</p>
             <a class="btn btn-primary" href="shop.html">Shop Healthy Treats</a>`
    },
    'adoption-day': {
      title: 'Making Adoption Day Stress-Free',
      meta: 'Adoption · Jul 14, 2026',
      image: 'https://images.unsplash.com/photo-1601758228041-f3b2795255f1?auto=format&fit=crop&w=900&q=85',
      body: `<p>Bringing a new pet home is exciting — a little preparation makes the transition smoother for everyone.</p>
             <h2>1. Pet-proof your space</h2><p>Set up a quiet, safe area with food, water and bedding ready in advance.</p>
             <h2>2. Go slow on introductions</h2><p>Give your new pet time to explore and adjust at their own pace.</p>
             <h2>3. Keep the first few days calm</h2><p>Limit visitors and loud activity while your pet settles in.</p>
             <a class="btn btn-primary" href="adoption.html">Meet Adoptable Pets</a>`
    }
  };

  const params = new URLSearchParams(window.location.search);
  const id = params.get('id') || 'dog-health';
  const article = ARTICLES[id] || ARTICLES['dog-health'];

  document.title = article.title + ' | FurShield';
  const set = (elId, value) => { const el = document.getElementById(elId); if (el) el.textContent = value; };

  set('blogBreadcrumb', `Home / Blog / ${article.title}`);
  set('blogMeta', article.meta);
  set('blogTitle', article.title);

  const img = document.getElementById('blogImage');
  if (img) { img.src = article.image; img.alt = article.title; }

  const body = document.getElementById('blogBody');
  if (body) body.innerHTML = article.body;
});
