document.addEventListener('DOMContentLoaded', () => {
  const PETS = {
    charlie: {
      name: 'Charlie', age: '2 Years', breed: 'Golden Retriever', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1558788353-f76d92427f16?auto=format&fit=crop&w=900&q=85',
      tagline: 'A gentle companion with a big heart.',
      bio: 'Charlie loves people, walks and playful afternoons. He is looking for a family ready to give him a loving forever home.'
    },
    luna: {
      name: 'Luna', age: '1 Year', breed: 'Domestic Cat', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1518791841217-8f162f1e1131?auto=format&fit=crop&w=900&q=85',
      tagline: 'Curious, affectionate and ready for a quiet forever home.',
      bio: 'Luna is a gentle soul who loves sunny windowsills and quiet cuddles. She would thrive in a calm, loving household.'
    },
    max: {
      name: 'Max', age: '3 Years', breed: 'Labrador Mix', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1601758228041-f3b2795255f1?auto=format&fit=crop&w=900&q=85',
      tagline: 'Loyal, social and always ready for a walk.',
      bio: 'Max is a friendly, energetic companion who gets along well with kids and other pets. He loves long walks and playtime.'
    },
    milo: {
      name: 'Milo', age: '2 Years', breed: 'Friendly Cat', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1533738363-b7f9aef128ce?auto=format&fit=crop&w=900&q=85',
      tagline: 'Bright, playful and full of personality.',
      bio: 'Milo is a sociable cat who enjoys interactive toys and gentle attention. He is looking for a patient, loving family.'
    },
    buddy: {
      name: 'Buddy', age: '4 Years', breed: 'Mixed Breed', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1587300003388-59208cc962cb?auto=format&fit=crop&w=900&q=85',
      tagline: 'A steady, loving presence for any home.',
      bio: 'Buddy is a calm, well-mannered dog who is great with children and enjoys relaxed evenings just as much as active days.'
    },
    coco: {
      name: 'Coco', age: '1 Year', breed: 'Rescue Cat', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1495360010541-f48722b34f7d?auto=format&fit=crop&w=900&q=85',
      tagline: 'A resilient little cat ready for a fresh start.',
      bio: 'Coco was rescued as a kitten and has grown into a sweet, trusting companion. She is ready to settle into a forever home.'
    },
    nibbles: {
      name: 'Nibbles', age: '8 Months', breed: 'Holland Lop Rabbit', location: 'Karachi',
      image: 'https://images.unsplash.com/photo-1585110396000-c9ffd4e4b308?auto=format&fit=crop&w=900&q=85',
      tagline: 'A gentle, quiet companion who loves a calm home.',
      bio: 'Nibbles is a curious, easy-going rabbit who enjoys fresh greens, quiet cuddle time, and a safe space to hop around. Looking for a patient family who understands rabbit care.'
    }
  };

  const params = new URLSearchParams(window.location.search);
  const id = (params.get('id') || 'charlie').toLowerCase();
  const pet = PETS[id] || PETS.charlie;

  document.title = pet.name + ' | FurShield';
  const set = (elId, value) => { const el = document.getElementById(elId); if (el) el.textContent = value; };

  set('petBreadcrumb', `Home / Adoption / ${pet.name}`);
  set('petHeading', `Meet ${pet.name}`);
  set('petName', pet.name);
  set('petNameInline', pet.name);
  set('petAge', pet.age);
  set('petBreed', pet.breed);
  set('petLocation', pet.location);
  set('petTagline', pet.tagline);
  set('petBio', pet.bio);

  const img = document.getElementById('petImage');
  if (img) { img.src = pet.image; img.alt = pet.name; }
});
