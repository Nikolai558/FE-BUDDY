// FE-Buddy Alias Command Practice: shows each question, reads the command the controller typed,
// and says whether it is right - and, when it is not, what looks missing or out of place. The
// questions are the JSON in #practice-data, written by FE-Buddy, every accepted command broken
// into its parts: typed, airport, identifier, approachType, variant, runway and page.
(function () {
  'use strict';

  const data = JSON.parse(document.getElementById('practice-data').textContent);
  const typeNames = data.typeNames;

  const pillClass = {
    airport: 'k-airport', identifier: 'k-ident', approachType: 'k-type',
    variant: 'k-variant', runway: 'k-runway', page: 'k-page',
  };

  const cardNames = { '.apt': "an airport's card", '.nav': "a NAVAID's card", '.id': "an aircraft operator's card" };

  const identifierLabel = {
    departure: "the departure's name", arrival: "the arrival's name", visual: "the approach's name",
    otherChart: 'the chart code', airway: 'the airway ID', navaid: "the NAVAID's ID", operator: "the operator's 3LD",
  };

  const outcome = { 'Display the ISR': 'displays the card.', 'Display the fixes': 'displays the fixes.', 'Recall the chart': 'recalls the chart.' };

  // ------------------------------------------------------------------ helpers

  const escape = text => text.replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]);

  /** A value in a hint: a pill in its kind's colour, or plain code for typed text. */
  const show = (text, kind) => pillClass[kind]
    ? `<code class="cmd"><span class="part ${pillClass[kind]}">${escape(text)}</span></code>`
    : `<code>${escape(text)}</code>`;

  const flat = parts => parts.map(part => part.t).join('');

  /** "a", "a and b", "a, b and c". */
  const joinList = items => items.length < 3
    ? items.join(' and ')
    : `${items.slice(0, -1).join(', ')} and ${items[items.length - 1]}`;

  /** How many single-character edits turn one string into the other. */
  function distance(a, b) {
    let row = Array.from({ length: b.length + 1 }, (_, j) => j);
    for (let i = 1; i <= a.length; i++) {
      const next = [i];
      for (let j = 1; j <= b.length; j++) {
        next[j] = Math.min(row[j] + 1, next[j - 1] + 1, row[j - 1] + (a[i - 1] === b[j - 1] ? 0 : 1));
      }
      row = next;
    }
    return row[b.length];
  }

  // ---------------------------------------------------------------- diagnosis

  /**
   * Lines the expected command up with what was typed, character by character (their longest
   * common subsequence): '=' a character in both, '-' one missing from what was typed, '+' an
   * extra one. When either would do, the expected character is the one dropped, so what was typed
   * lines up with the end of the command and its c or f stays matched.
   */
  function align(expected, typed) {
    const n = expected.length, m = typed.length;
    const lcs = Array.from({ length: n + 1 }, () => new Array(m + 1).fill(0));
    for (let i = n - 1; i >= 0; i--) {
      for (let j = m - 1; j >= 0; j--) {
        lcs[i][j] = expected[i] === typed[j] ? lcs[i + 1][j + 1] + 1 : Math.max(lcs[i + 1][j], lcs[i][j + 1]);
      }
    }

    const ops = [];
    let i = 0, j = 0;
    while (i < n && j < m) {
      if (expected[i] === typed[j]) ops.push({ op: '=', i: i++, j: j++ });
      else if (lcs[i + 1][j] >= lcs[i][j + 1]) ops.push({ op: '-', i: i++ });
      else ops.push({ op: '+', j: j++ });
    }
    while (i < n) ops.push({ op: '-', i: i++ });
    while (j < m) ops.push({ op: '+', j: j++ });
    return ops;
  }

  /**
   * What was typed for each part of the expected command. A matched character goes to its part.
   * A run of extra characters goes to the part whose characters it replaced, when it sits beside
   * some missing ones (the v of .nav where .apt's p and t are missing); between two whole parts
   * that are both missing, to the first one of its length (an I typed for RNAV's R, ahead of its
   * variant). Otherwise it goes to the part it sits in or, between two parts, to the one before it
   * (version digits after a name, the LS of ILS after an I) - unless that is typed text or the
   * airport, when it goes to the one after (the K of an ICAO ID, the V of VOR spelled out).
   */
  function split(parts, typed) {
    const expected = parts.map(part => part.t.toLowerCase());
    const whole = expected.join('');
    const owner = expected.flatMap((text, p) => Array.from(text, () => p));
    const ops = align(whole, typed);
    const got = expected.map(() => '');
    let matched = 0;

    const ownerOf = (prev, next) => {
      const before = prev >= 0 ? owner[prev] : -1;
      const after = next < whole.length ? owner[next] : -1;
      if (before === after || after < 0) return before;
      if (before < 0 || parts[before].k === 'typed' || parts[before].k === 'airport') return after;
      return before;
    };

    const runOwner = (run, missing, prev, next) => {
      const owners = [...new Set(missing.map(i => owner[i]))];
      if (owners.length === 1) return owners[0];
      const replaced = owners.filter(p => expected[p].length === run.length && missing.filter(i => owner[i] === p).length === run.length);
      return replaced.length ? replaced[0] : ownerOf(prev, next);
    };

    let k = 0;
    while (k < ops.length) {
      const o = ops[k];

      if (o.op !== '+') {
        if (o.op === '=') {
          got[owner[o.i]] += typed[o.j];
          matched++;
        }
        k++;
        continue;
      }

      let end = k;
      while (end < ops.length && ops[end].op === '+') end++;
      const run = ops.slice(k, end).map(op => typed[op.j]).join('');

      // The expected characters missing right beside the run: those it may have replaced.
      const missing = [];
      for (let b = k - 1; b >= 0 && ops[b].op === '-'; b--) missing.unshift(ops[b].i);
      if (!missing.length) for (let f = end; f < ops.length && ops[f].op === '-'; f++) missing.push(ops[f].i);

      let prev = -1, next = whole.length;
      for (let b = k - 1; b >= 0; b--) if (ops[b].op !== '+') { prev = ops[b].i; break; }
      for (let f = end; f < ops.length; f++) if (ops[f].op !== '+') { next = ops[f].i; break; }

      got[missing.length ? runOwner(run, missing, prev, next) : ownerOf(prev, next)] += run;
      k = end;
    }

    return { expected, got, matched: matched / whole.length };
  }

  /** A part typed in the wrong place. */
  function movedHint(parts, p) {
    const part = parts[p];
    if (part.k === 'page' || (part.t === 'c' && parts[p + 1] && parts[p + 1].k === 'page')) {
      return `The page number goes after the ${show('c')}.`;
    }
    if (part.k === 'variant') {
      return `The variant ${show(part.t, 'variant')} goes straight after the approach type code, before the runway.`;
    }
    if (part.t === 'v') return `The ${show('v')} goes straight after the airport ID.`;
    return `${show(part.t, part.k)} is in the wrong place.`;
  }

  /**
   * What looks wrong with a typed command, compared with the accepted one it is closest to:
   * everything left out in one "Looks like you forgot to include..." sentence, then anything
   * else, part by part. Null when what was typed is too far off to read part by part.
   */
  function explain(typed, parts, q) {
    const { expected, got, matched } = split(parts, typed);
    if (matched < 0.5) return null;

    // Missing where it belongs, and extra beside another part: typed in the wrong place.
    const moved = new Set();
    expected.forEach((text, p) => {
      if (got[p] !== '') return;
      const r = got.findIndex((g, other) => other !== p && (g === expected[other] + text || g === text + expected[other]));
      if (r >= 0) {
        got[r] = expected[r];
        moved.add(p);
      }
    });

    const forgot = [], hints = [];
    const hasRunway = parts.some(part => part.k === 'runway');
    const hasVariant = parts.some(part => part.k === 'variant');

    parts.forEach((part, p) => {
      const e = expected[p], g = got[p], shown = part.t;

      if (moved.has(p)) {
        hints.push(movedHint(parts, p));
        return;
      }

      if (g === e) return;

      switch (part.k) {
        case 'typed': {
          const last = parts.slice(p + 1).every(next => next.k === 'page');
          if (e === '.') hints.push('Every alias command starts with a period.');
          else if (cardNames[e]) {
            hints.push(cardNames[g]
              ? `${show(g)} displays ${cardNames[g]}. For ${cardNames[e]}, start with ${show(e)}.`
              : `For ${cardNames[e]}, start with ${show(e)}.`);
          } else if (e === 'v' && g === '') forgot.push(`the lower-case ${show('v')} (for Visual) after the airport ID`);
          else if (e === 'c' && g === 'f') hints.push(`Ending in ${show('f')} displays a procedure's fixes. To recall the chart, end with ${show('c')}.`);
          else if (e === 'f' && g === 'c') hints.push(`Ending in ${show('c')} recalls the chart. To display the fixes, end with ${show('f')}.`);
          else if (g === '') forgot.push(last ? `the ${show(shown)} at the end` : `the ${show(shown)} before the page number`);
          else if (last && g.startsWith(e)) hints.push(`The command ends with ${show(shown)}: leave out ${show(g.slice(e.length))}.`);
          else hints.push(`Type ${show(shown)} here, exactly as shown.`);
          break;
        }

        case 'airport':
          if (g === '') forgot.push(`the airport ID ${show(shown, 'airport')}`);
          else if (g === `k${e}`) hints.push(`Use the airport's FAA ID ${show(shown, 'airport')}, not its ICAO ID ${show(`K${shown.toUpperCase()}`, 'airport')}.`);
          else hints.push(`The airport ID here is ${show(shown, 'airport')}.`);
          break;

        case 'approachType': {
          const name = typeNames[shown];
          const letters = text => text.replace(/[^a-z]/gi, '').toLowerCase();
          const spelledOut = Object.keys(typeNames).find(code => letters(typeNames[code]) === letters(g));
          const typedCode = Object.keys(typeNames).find(code => code.toLowerCase() === g);
          const extra = g.startsWith(e) ? g.slice(e.length) : '';

          if (g === '') forgot.push(`the approach type code ${show(shown, 'approachType')} (${name})`);
          else if (spelledOut === shown) hints.push(`Use the approach type code ${show(shown, 'approachType')}, not the word ${name}.`);
          else if (e === 'r' && g === 'g') {
            hints.push(`RNAV is ${show('R', 'approachType')} whatever its brackets say, (GPS) or (RNP). `
              + `${show('G', 'approachType')} is only for a GPS approach with no RNAV in its name.`);
          } else if (e === 'g' && g === 'r') hints.push(`This is a GPS approach, with no RNAV in its name, so its code is ${show('G', 'approachType')}.`);
          else if (e.endsWith('bc') && g === e.slice(0, -2)) forgot.push(`the back course ${show('BC', 'approachType')} after ${show(shown.slice(0, -2), 'approachType')}`);
          else if (e.length > 1 && e[1] === 'd' && g === e[0] + e.slice(2)) forgot.push(`the ${show('D', 'approachType')} a /DME approach adds after ${show(shown[0], 'approachType')}`);
          else if (!hasVariant && /^[a-z]$/.test(extra)) hints.push(`This approach has no variant letter: leave out the ${show(extra.toUpperCase(), 'variant')}.`);
          else if (spelledOut || typedCode) {
            const code = spelledOut || typedCode;
            hints.push(`The approach type code here is ${show(shown, 'approachType')} (${name}); ${show(code, 'approachType')} is ${typeNames[code]}.`);
          } else hints.push(`The approach type code here is ${show(shown, 'approachType')} (${name}).`);
          break;
        }

        case 'variant':
          if (g === '') forgot.push(hasRunway ? `the variant ${show(shown, 'variant')}` : `the circling letter ${show(shown, 'variant')}`);
          else hints.push(`The ${hasRunway ? 'variant' : 'circling letter'} here is ${show(shown, 'variant')}.`);
          break;

        case 'runway': {
          const wanted = /^(\d*)([a-z]?)$/.exec(e) || ['', e, ''];
          const typedRunway = /^(\d*)([lcr]?)(.*)$/.exec(g);
          const [, number, side] = wanted;
          const [, typedNumber, typedSide, rest] = typedRunway;

          if (g === '') {
            forgot.push(`the runway ${show(shown, 'runway')}`);
            break;
          }

          if (rest) {
            hints.push(`The runway is ${show(shown, 'runway')}, exactly as the chart prints it.`);
            break;
          }

          if (typedNumber !== number) {
            const lost = typedNumber && number.endsWith(typedNumber) ? number.slice(0, number.length - typedNumber.length) : null;
            if (lost !== null && /^0+$/.test(lost)) hints.push(`Keep the runway's leading zero: ${show(shown, 'runway')}, as the chart prints it.`);
            else if (lost !== null) forgot.push(`the full runway number ${show(number, 'runway')}`);
            else if (!typedNumber) forgot.push(`the runway number ${show(number, 'runway')}`);
            else {
              hints.push(`The runway here is ${show(shown, 'runway')}.`);
              break;
            }
          }

          if (typedSide !== side) {
            if (!typedSide) forgot.push(`the runway ID suffix ${show(side.toUpperCase(), 'runway')}`);
            else if (side) hints.push(`The runway is ${show(shown, 'runway')}: ${side.toUpperCase()}, not ${typedSide.toUpperCase()}.`);
            else hints.push(`This runway has no L, C or R: it's just ${show(shown, 'runway')}.`);
          }
          break;
        }

        case 'identifier': {
          const procedure = q.subject === 'departure' || q.subject === 'arrival';
          const extra = g.startsWith(e) ? g.slice(e.length) : '';
          if (g === '') forgot.push(`${identifierLabel[q.subject] || 'the name'} ${show(shown, 'identifier')}`);
          else if (procedure && /^\d+$/.test(extra)) hints.push(`Leave out the version number: ${show(shown, 'identifier')}, not ${show(g.toUpperCase(), 'identifier')}.`);
          else if (q.subject === 'visual' && e.startsWith(g)) hints.push(`Spell the approach's name out in full: ${show(shown, 'identifier')}, not ${show(g.toUpperCase(), 'identifier')}.`);
          else hints.push(q.identifierHint || `It should be ${show(shown, 'identifier')}.`);
          break;
        }

        default:
          if (g === '') forgot.push(`the page number ${show(shown, 'page')} after the ${show('c')}`);
          else hints.push(`This is page ${show(shown, 'page')}.`);
      }
    });

    if (forgot.length) hints.unshift(`Looks like you forgot to include ${joinList(forgot)}.`);
    return hints;
  }

  /**
   * Checks a typed command against a question: { correct, answer, hints }, where answer is the
   * accepted command it matched or is closest to, and hints are HTML. Spaces at either end are
   * trimmed and case is ignored, as CRC ignores it.
   */
  function diagnose(input, q) {
    const notes = [];
    let typed = input.trim().toLowerCase();

    if (/\s/.test(typed)) {
      notes.push('Leave out the spaces: CRC reads an alias command as one word.');
      typed = typed.replace(/\s+/g, '');
    }

    if (q.subject === 'visual' && typed.includes('visual')) {
      notes.push(`Leave out the word VISUAL: the lower-case ${show('v')} stands for it.`);
      typed = typed.replace('visual', 'v');
    }

    if ((q.subject === 'visual' || q.subject === 'approach') && typed.includes('rwy')) {
      notes.push('Leave out the word RWY: the runway follows straight on.');
      typed = typed.replace(/rwy/g, '');
    }

    const answers = q.answers.map(answer => flat(answer.parts).toLowerCase());
    const nearest = text => Math.min(...answers.map(answer => distance(answer, text)));

    // A c where an f goes, or an f where a c goes - even when the name before it ends in c too.
    const endings = [];
    const action = answers[0].slice(-1);
    const swappedAction = { c: 'f', f: 'c' }[action];
    if (swappedAction && typed.endsWith(swappedAction) && nearest(typed.slice(0, -1) + action) < nearest(typed)) {
      endings.push(action === 'c'
        ? `Ending in ${show('f')} displays a procedure's fixes. To recall the chart, end with ${show('c')}.`
        : `Ending in ${show('c')} recalls the chart. To display the fixes, end with ${show('f')}.`);
      typed = typed.slice(0, -1) + action;
    }

    const exact = answers.indexOf(typed);

    if (exact >= 0) {
      const hints = notes.concat(endings);
      return { correct: hints.length === 0, answer: exact, hints };
    }

    let best = 0;
    answers.forEach((answer, i) => {
      if (distance(answer, typed) < distance(answers[best], typed)) best = i;
    });

    const hints = explain(typed, q.answers[best].parts, q);

    return hints
      ? { correct: false, answer: best, hints: notes.concat(hints, endings) }
      : { correct: false, answer: 0, hints: notes.concat(endings, ["That isn't close to this command. Here's how it's built:"]) };
  }

  // --------------------------------------------------------------------- page

  const byId = id => document.getElementById(id);
  const questions = data.questions;
  const input = byId('answer');

  let order = [];          // the questions being practised, by index, shuffled
  let at = 0;              // which of them is showing
  let firstTries = new Map(); // question index -> right on the first try
  let checkedText = null;  // what was in the box when it was last checked

  function shuffle(items) {
    for (let i = items.length - 1; i > 0; i--) {
      const j = Math.floor(Math.random() * (i + 1));
      [items[i], items[j]] = [items[j], items[i]];
    }
    return items;
  }

  function score() {
    const right = [...firstTries.values()].filter(Boolean).length;
    const wrong = firstTries.size - right;
    byId('score').textContent = `${right} right · ${wrong} wrong · ${order.length - firstTries.size} to go`;
  }

  function start(indices) {
    order = shuffle(indices.slice());
    at = 0;
    firstTries = new Map();
    render();
  }

  function render() {
    score();

    if (at >= order.length) {
      const right = [...firstTries.values()].filter(Boolean).length;
      byId('quiz').hidden = true;
      byId('done').hidden = false;
      byId('final').textContent = `You got ${right} of ${order.length} right on the first try.`;
      byId('missed').hidden = right === order.length;
      return;
    }

    const q = questions[order[at]];
    byId('quiz').hidden = false;
    byId('done').hidden = true;
    byId('progress').textContent = `Question ${at + 1} of ${order.length}`;
    byId('section').textContent = q.section;
    byId('action').textContent = q.action;
    byId('name').textContent = q.name;
    byId('detail').textContent = q.detail;
    byId('result').hidden = true;
    input.value = '';
    checkedText = null;
    input.focus();
  }

  function check() {
    const index = order[at];
    const q = questions[index];
    const verdict = byId('verdict');
    byId('result').hidden = false;

    if (input.value.trim() === '') {
      verdict.className = 'verdict';
      verdict.textContent = 'Type the command, then press Enter.';
      byId('details').hidden = true;
      return;
    }

    const result = diagnose(input.value, q);
    const answer = q.answers[result.answer];
    const others = q.answers.filter((_, i) => i !== result.answer);

    if (!firstTries.has(index)) firstTries.set(index, result.correct);
    checkedText = input.value;

    verdict.className = `verdict ${result.correct ? 'good' : 'bad'}`;
    verdict.textContent = result.correct ? '✓ Correct!' : '✗ Not quite.';
    byId('hints').innerHTML = result.hints.map(hint => `<li>${hint}</li>`).join('');
    byId('hints').hidden = result.hints.length === 0;
    byId('correct').innerHTML = (result.correct ? `${answer.html} ${outcome[q.action]}` : `The correct command is ${answer.html}.`)
      + (others.length ? ` ${others.length === 1 ? 'This one works too' : 'These work too'}: ${others.map(other => other.html).join(', ')}.` : '');
    byId('breakdown').innerHTML = answer.breakdown.map(row => `<tr><td>${row.html}</td><td>${escape(row.text)}</td></tr>`).join('');
    byId('notes').innerHTML = q.notes.map(note => `<li>${note}</li>`).join('');
    byId('notes-box').hidden = q.notes.length === 0;
    byId('details').hidden = false;
    score();
  }

  function next() {
    at++;
    render();
  }

  // Enter checks the command; Enter again, with it unchanged, moves on.
  byId('form').addEventListener('submit', event => {
    event.preventDefault();
    if (checkedText !== null && input.value === checkedText) next();
    else check();
  });

  byId('next').addEventListener('click', next);
  byId('again').addEventListener('click', () => start(order));
  byId('missed').addEventListener('click', () => start(order.filter(index => !firstTries.get(index))));

  document.querySelectorAll('[data-section]').forEach(chip => chip.addEventListener('click', () => {
    document.querySelectorAll('[data-section]').forEach(other => other.setAttribute('aria-pressed', String(other === chip)));
    const section = chip.dataset.section;
    start(questions.map((_, i) => i).filter(i => !section || questions[i].section === section));
  }));

  window.feBuddyPractice = { diagnose, questions };
  start(questions.map((_, i) => i));
})();
