import React, { useEffect } from 'react';
import clsx from 'clsx';
import Link from '@docusaurus/Link';
import useDocusaurusContext from '@docusaurus/useDocusaurusContext';
import Layout from '@theme/Layout';
import HomepageFeatures from '@site/src/components/HomepageFeatures';

import styles from './index.module.css';

const FeatureSection = ({ imageUrl, title, description, isReversed }) => {
  return (
    <div className={clsx(styles.featureSection, isReversed && styles.reversed)}>
      <div className={styles.featureContent}>
        <h2>{title}</h2>
        <p>{description}</p>
      </div>
      <div className={styles.featureImage}>
        <img src={imageUrl} alt={title} />
      </div>
    </div>
  );
};

function HomepageHeader() {
  const {siteConfig} = useDocusaurusContext();
  return (
    <header className={clsx('hero hero--primary', styles.heroBanner)}>
      <div className={clsx('container', styles.parallaxContainer)}>
        <div className={styles.logoWrapper}>
          <img 
            src='img/landing-page/fig_logo_name_right.svg'
            className={styles.parallaxLogo}
          />
        </div>
        <p className={clsx("hero__subtitle", styles.parallaxText)}>{siteConfig.tagline}</p>
        <div className={styles.buttons}>
          <Link
            className="button button--secondary button--lg"
            to="/docs/intro">
            Get Started with Fig
          </Link>
        </div>
      </div>
    </header>
  );
}

export default function Home() {
  const {siteConfig} = useDocusaurusContext();

  useEffect(() => {
    const handleScroll = () => {
      const elements = document.querySelectorAll(`.${styles.featureSection}`);
      elements.forEach(element => {
        const rect = element.getBoundingClientRect();
        const isVisible = rect.top < window.innerHeight && rect.bottom >= 0;
        if (isVisible) {
          element.classList.add(styles.visible);
        }
      });
    };

    window.addEventListener('scroll', handleScroll);
    handleScroll();
    return () => window.removeEventListener('scroll', handleScroll);
  }, []);

  return (
    <Layout
      title={`${siteConfig.title}`}
      description="Centralized settings management for dotnet microservices">
      <HomepageHeader />
      <main>
        <div className={styles.screenshotContainer}>
          <img 
            src="/img/landing-page/app-screenshot.png" 
            alt="Fig Application Screenshot" 
            className={styles.screenshot}
          />
        </div>
        <div className={styles.featuresContainer}>
          <FeatureSection
            imageUrl="/img/landing-page/central-management.webp"
            title="Centralized Configuration"
            description="Manage all your microservice settings in one place. Real-time updates ensure your services stay in sync."
          />
          <FeatureSection
            imageUrl="/img/landing-page/easy-to-integrate.png"
            title="Easy to Integrate"
            description="Plugs directly into the ASP.NET configuration system, so it integrates seamlessly with your existing setup."
            isReversed
          />
          <FeatureSection
            imageUrl="/img/landing-page/setting-quality.png"
            title="Enhanced Configuration Quality"
            description="Typed settings, validation, lookup tables and custom verifications improve the configuration experience and reduces errors."
          />
          <FeatureSection
            imageUrl="/img/landing-page/client-management.png"
            title="Client Status Tracking"
            description="Update settings in real time without restarts and remotely monitor the status of your services."
            isReversed
          />
          <FeatureSection
            imageUrl="/img/landing-page/event-history.png"
            title="Full Audit History"
            description="All changes are logged in an immutable history including who made the change, when and what was changed."
          />
          <FeatureSection
            imageUrl="/img/landing-page/export.png"
            title="Import / Export Support"
            description="Import and export settings to json to easily move configurations between environments."
            isReversed
          />
           <FeatureSection
            imageUrl="/img/landing-page/secure.webp"
            title="Secure"
            description="All setting values are encrypted at rest, secrets are not sent to the web application and clients must authenticate with their own secret to get settings."
          />
          <FeatureSection
            imageUrl="/img/landing-page/highly-available.webp"
            title="Highly Available"
            description="Deploy the stateless api in multiple locations for high availability and local encrypted cache ensures clients can even start without reaching the api."
            isReversed
          />
          <FeatureSection
            imageUrl="/img/landing-page/web-hooks.png"
            title="Feature Rich"
            description="Packed full of features like web hooks, built in setting documentation, grouping, scriptable validation and much more."
          />
          <FeatureSection
            imageUrl="/img/landing-page/open-source.png"
            title="Open Source"
            description="Open sourced on GitHub and licenced under an Apache 2.0 license."
            isReversed
          />
        </div>
      </main>
    </Layout>
  );
}
